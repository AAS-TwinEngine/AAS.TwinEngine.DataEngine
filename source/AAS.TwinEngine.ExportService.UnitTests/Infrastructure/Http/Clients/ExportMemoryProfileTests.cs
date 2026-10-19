using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;

using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit.Abstractions;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Clients;

[CollectionDefinition("ExportMemoryProfile", DisableParallelization = true)]
public sealed class ExportMemoryProfileCollection;

public sealed class MemoryProfileTheoryAttribute : TheoryAttribute
{
    public MemoryProfileTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("EXPORT_MEMORY_PROFILE") != "1")
        {
            Skip = "Opt-in profile: set EXPORT_MEMORY_PROFILE=1 and run in Release configuration.";
        }
    }
}

[Collection("ExportMemoryProfile")]
[Trait("Category", "MemoryProfile")]
public sealed class ExportMemoryProfileTests(ITestOutputHelper output)
{
    private static readonly int MeasuredRuns = int.Parse(
        Environment.GetEnvironmentVariable("EXPORT_MEMORY_PROFILE_RUNS") ?? "5", CultureInfo.InvariantCulture);
    private const long RetainedGrowthBudget = 8 * 1024 * 1024;

    [MemoryProfileTheory]
    [InlineData(4)]
    [InlineData(64)]
    [InlineData(256)]
    public async Task ReadPagesAsync_WhenHolding250Submodels_MeasuresListAndReleasesPayloads(int payloadKiB)
    {
        using var fixture = new StreamedSubmodelFixture(250, payloadKiB * 1024);
        var config = new ExportServiceConfig();
        config.Sources.Submodels.Path = "/submodels";
        var reader = new SourceEntityHttpClient(fixture, Options.Create(config), NullLogger<SourceEntityHttpClient>.Instance);
        _ = await MeasureHeldPageAsync(reader);

        var measurement = await MeasureHeldPageAsync(reader);
        _ = CollectHeap();

        Assert.False(measurement.ListReference.IsAlive, "The source list remains rooted after its reader is disposed.");
        Assert.False(measurement.PayloadReference.IsAlive, "Source payload remains rooted after releasing its list.");
        output.WriteLine("HELD_PAGE " + JsonSerializer.Serialize(new
        {
            payloadKiB, measurement.PayloadUtf16Bytes, measurement.ListReferenceBytes,
            measurement.HeapBefore, measurement.HeapWhileHeld,
            retainedWhileHeldBytes = measurement.HeapWhileHeld - measurement.HeapBefore,
            listReleased = !measurement.ListReference.IsAlive,
            payloadReleased = !measurement.PayloadReference.IsAlive
        }));
    }

    private static async Task<HeldPageMeasurement> MeasureHeldPageAsync(SourceEntityHttpClient reader)
    {
        var before = CollectHeap();
        var pages = reader.ReadPagesAsync(EntityKind.Submodel, CancellationToken.None).GetAsyncEnumerator();
        HeldPageMeasurement measurement;
        try
        {
            Assert.True(await pages.MoveNextAsync());
            var page = Assert.IsType<List<SourceEntity>>(pages.Current);
            Assert.Equal(250, page.Count);
            var payloadBytes = page.Sum(entity => (long)entity.RawJson.Length * sizeof(char));
            var held = CollectHeap();
            measurement = new HeldPageMeasurement(before, held, payloadBytes,
                (long)page.Capacity * IntPtr.Size, new WeakReference(page), new WeakReference(page[0].RawJson));
            GC.KeepAlive(page);
            page = null!;
        }
        finally
        {
            await pages.DisposeAsync();
        }

        pages = null!;
        await Task.Yield();
        return measurement;
    }

    private sealed record HeldPageMeasurement(
        long HeapBefore, long HeapWhileHeld, long PayloadUtf16Bytes, long ListReferenceBytes,
        WeakReference ListReference, WeakReference PayloadReference);

    [MemoryProfileTheory]
    [InlineData(250, 4, "create")]
    [InlineData(250, 64, "create")]
    [InlineData(250, 256, "create")]
    [InlineData(5000, 4, "create")]
    [InlineData(20000, 4, "create")]
    [InlineData(250, 4, "unchanged")]
    [InlineData(250, 64, "unchanged")]
    [InlineData(250, 256, "unchanged")]
    [InlineData(5000, 4, "unchanged")]
    [InlineData(20000, 4, "unchanged")]
    [InlineData(250, 64, "update")]
    public async Task ExecuteAsync_With250SubmodelPages_ProfilesMemoryAndRepeatedRunRetention(
        int entityCount, int payloadKiB, string mode)
    {
        Assert.InRange(MeasuredRuns, 5, 100);
        using var fixture = new StreamedSubmodelFixture(entityCount, payloadKiB * 1024);
        var state = new ProfileStateStore();
        var config = new ExportServiceConfig();
        config.Sources.Submodels.Path = "/submodels";
        config.Sources.Submodels.Limit = 250;
        config.Targets.Submodels.Path = "/submodels";
        config.Targets.OmitNullProperties = true;
        config.Performance.MaxDegreeOfParallelism = 10;
        var options = Options.Create(config);
        var reader = new SourceEntityHttpClient(fixture, options, NullLogger<SourceEntityHttpClient>.Instance);
        var writer = new TargetEntityHttpClient(fixture, options, NullLogger<TargetEntityHttpClient>.Instance);
        var executor = new PhaseExecutor(reader, writer, state, new CrudDecisionMaker(), options, NullLogger<PhaseExecutor>.Instance);

        for (var warmup = 0; warmup < 2; warmup++)
        {
            state.Prepare(mode);
            await executor.ExecuteAsync(EntityKind.Submodel, CancellationToken.None);
        }

        output.WriteLine($"ENVIRONMENT {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; " +
            $"CPU={Environment.ProcessorCount}; ServerGC={System.Runtime.GCSettings.IsServerGC}; " +
            $"PageSize=250; Parallelism=10; Runs={MeasuredRuns}; RetainedGrowthBudgetMiB=8");

        var retained = new List<long>(MeasuredRuns);
        for (var iteration = 1; iteration <= MeasuredRuns; iteration++)
        {
            state.Prepare(mode);
            fixture.ResetCounters();
            var heapBefore = CollectHeap();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var gen2Before = GC.CollectionCount(2);
            using var process = Process.GetCurrentProcess();
            var cpuBefore = process.TotalProcessorTime;
            await using var sampler = new MemorySampler();
            var stopwatch = Stopwatch.StartNew();

            var result = await executor.ExecuteAsync(EntityKind.Submodel, CancellationToken.None);

            stopwatch.Stop();
            var cpuMilliseconds = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
            await sampler.StopAsync();
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            var gen2Collections = GC.CollectionCount(2) - gen2Before;
            var heapAfter = CollectHeap();
            retained.Add(heapAfter);

            Assert.Equal(0, result.Failed);
            Assert.Equal(mode == "create" ? entityCount : 0, result.Created);
            Assert.Equal(mode == "update" ? entityCount : 0, result.Updated);
            Assert.Equal(mode == "unchanged" ? entityCount : 0, result.Skipped);
            Assert.Equal(mode == "unchanged" ? 0 : entityCount, fixture.Writes);
            Assert.Equal(entityCount, state.Count);
            Assert.Equal((entityCount + 249) / 250, fixture.Reads);

            output.WriteLine("PROFILE " + JsonSerializer.Serialize(new
            {
                entityCount, payloadKiB, mode, iteration,
                elapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                entitiesPerSecond = entityCount / stopwatch.Elapsed.TotalSeconds,
                cpuMilliseconds, allocatedBytes = allocated, heapBefore, heapAfter,
                sampledPeakHeap = sampler.PeakHeap, sampledPeakWorkingSet = sampler.PeakWorkingSet,
                gen2Collections, errors = result.Failed
            }));
        }

        var retainedGrowth = retained.Skip(1).Max() - retained[0];
        output.WriteLine($"RETENTION growthBytes={retainedGrowth}; budgetBytes={RetainedGrowthBudget}");
        Assert.True(retainedGrowth <= RetainedGrowthBudget,
            $"Post-warmup retained heap grew by {retainedGrowth} bytes; inspect a heap dump before calling this a leak.");
    }

    private static long CollectHeap()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        return GC.GetTotalMemory(forceFullCollection: false);
    }

    private sealed class MemorySampler : IAsyncDisposable
    {
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _sampling;
        public long PeakHeap { get; private set; }
        public long PeakWorkingSet { get; private set; }

        public MemorySampler() => _sampling = Task.Run(SampleAsync);

        private async Task SampleAsync()
        {
            using var process = Process.GetCurrentProcess();
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
            try
            {
                do
                {
                    process.Refresh();
                    PeakHeap = Math.Max(PeakHeap, GC.GetTotalMemory(forceFullCollection: false));
                    PeakWorkingSet = Math.Max(PeakWorkingSet, process.WorkingSet64);
                }
                while (await timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false));
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
            }
        }

        public async Task StopAsync()
        {
            await _stop.CancelAsync().ConfigureAwait(false);
            await _sampling.ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _stop.Dispose();
        }
    }

    private sealed class ProfileStateStore : IStateStore
    {
        private readonly ConcurrentDictionary<string, ExportedEntity> _entities = new(StringComparer.Ordinal);
        public int Count => _entities.Count;

        public void Prepare(string mode)
        {
            if (mode == "create")
            {
                _entities.Clear();
            }
            else if (mode == "update")
            {
                foreach (var entity in _entities.Values)
                {
                    _entities[entity.Identifier] = entity with { ContentHash = "changed" };
                }
            }
        }

        public Task EnsureSchemaAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<ExportedEntity>> LoadAsync(EntityKind kind, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ExportedEntity>>(_entities.Values.ToArray());
        public Task<ExportedEntity?> GetAsync(EntityKind kind, string identifier, CancellationToken cancellationToken) =>
            Task.FromResult(_entities.GetValueOrDefault(identifier));
        public Task UpsertAsync(ExportedEntity entity, CancellationToken cancellationToken)
        {
            _entities[entity.Identifier] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("This profile must not delete entities.");
    }

    private sealed class StreamedSubmodelFixture : HttpMessageHandler, IHttpClientFactory
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "export-memory-" + Guid.NewGuid().ToString("N"));
        private readonly int _payloadBytes;
        private int _reads;
        private int _writes;
        public int Reads => Volatile.Read(ref _reads);
        public int Writes => Volatile.Read(ref _writes);

        public StreamedSubmodelFixture(int entityCount, int payloadBytes)
        {
            _payloadBytes = payloadBytes;
            Directory.CreateDirectory(_directory);
            var value = new string('x', payloadBytes);
            for (var offset = 0; offset < entityCount; offset += 250)
            {
                using var stream = File.Create(PagePath(offset));
                using var json = new Utf8JsonWriter(stream);
                json.WriteStartObject();
                json.WriteStartObject("paging_metadata");
                if (offset + 250 < entityCount)
                {
                    json.WriteString("cursor", (offset + 250).ToString(CultureInfo.InvariantCulture));
                }

                json.WriteEndObject();
                json.WriteStartArray("result");
                for (var index = offset; index < Math.Min(offset + 250, entityCount); index++)
                {
                    json.WriteStartObject();
                    json.WriteString("id", $"urn:profile:submodel:{index}");
                    json.WriteString("idShort", $"Submodel{index}");
                    json.WriteString("modelType", "Submodel");
                    json.WriteNull("description");
                    json.WriteStartArray("submodelElements");
                    json.WriteStartObject();
                    json.WriteString("idShort", "Measurement");
                    json.WriteString("modelType", "Property");
                    json.WriteString("valueType", "xs:string");
                    json.WriteString("value", value);
                    json.WriteEndObject();
                    json.WriteEndArray();
                    json.WriteEndObject();
                }

                json.WriteEndArray();
                json.WriteEndObject();
            }
        }

        private string PagePath(int offset) => Path.Combine(_directory, $"page-{offset}.json");

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false)
        {
            BaseAddress = new Uri("http://profile.test"),
            Timeout = TimeSpan.FromMinutes(2)
        };

        public void ResetCounters()
        {
            _reads = 0;
            _writes = 0;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
                Assert.Equal("250", query["limit"]);
                var offset = int.Parse(query["cursor"] ?? "0", CultureInfo.InvariantCulture);
                _ = Interlocked.Increment(ref _reads);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(File.OpenRead(PagePath(offset)))
                };
            }

            Assert.True(request.Method == HttpMethod.Post || request.Method == HttpMethod.Put);
            await using var content = await request.Content!.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = document.RootElement;
            Assert.StartsWith("urn:profile:submodel:", root.GetProperty("id").GetString());
            Assert.Equal("Submodel", root.GetProperty("modelType").GetString());
            Assert.False(root.TryGetProperty("description", out _));
            Assert.Equal(_payloadBytes, root.GetProperty("submodelElements")[0].GetProperty("value").GetString()!.Length);
            _ = Interlocked.Increment(ref _writes);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Directory.Delete(_directory, recursive: true);
            }

            base.Dispose(disposing);
        }
    }
}