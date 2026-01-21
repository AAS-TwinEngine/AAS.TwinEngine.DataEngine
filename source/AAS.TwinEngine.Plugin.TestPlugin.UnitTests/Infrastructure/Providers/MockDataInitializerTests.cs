using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers;

public class MockDataInitializerTests
{
    private readonly ILogger<MockDataInitializer> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly string _testDataDirectory;

    public MockDataInitializerTests()
    {
        _logger = Substitute.For<ILogger<MockDataInitializer>>();
        _hostEnvironment = Substitute.For<IHostEnvironment>();
        _testDataDirectory = CreateTestDataDirectory();
        _hostEnvironment.ContentRootPath.Returns(_testDataDirectory);
    }

    private static string CreateTestDataDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var dataDir = Path.Combine(tempDir, "Data");
        Directory.CreateDirectory(dataDir);
        return tempDir;
    }

    private void CreateTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDataDirectory, "Data", fileName);
        File.WriteAllText(filePath, content);
    }

    [Fact]
    public void Initialize_ValidFiles_LoadsDataIntoRegistry()
    {
        CreateTestFile("mock-metadata.json", "{ \"meta\": \"data\" }");
        CreateTestFile("mock-submodel-data.json", "{ \"submodel\": \"data\" }");

        var initializer = new MockDataInitializer(_hostEnvironment, _logger);
        initializer.Initialize(CancellationToken.None);

        Assert.NotNull(MockData.MetaData);
        Assert.NotNull(MockData.SubmodelData);
    }

    [Fact]
    public void Initialize_MissingMetadataFile_ThrowsFileNotFoundException()
    {
        CreateTestFile("mock-submodel-data.json", "{ \"submodel\": \"data\" }");

        var initializer = new MockDataInitializer(_hostEnvironment, _logger);

        var ex = Assert.Throws<FileNotFoundException>(() => initializer.Initialize(CancellationToken.None));
        Assert.Contains("file not found", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Initialize_InvalidJson_ThrowsInternalServerException()
    {
        CreateTestFile("mock-metadata.json", "{ invalid json");
        CreateTestFile("mock-submodel-data.json", "{ \"submodel\": \"data\" }");

        var initializer = new MockDataInitializer(_hostEnvironment, _logger);

        var ex = Assert.Throws<InternalServerException>(() => initializer.Initialize(CancellationToken.None));
        Assert.Contains(ExceptionMessages.ResourceNotValid, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Initialize_EmptyFile_ThrowsInternalServerException()
    {
        CreateTestFile("mock-metadata.json", "");
        CreateTestFile("mock-submodel-data.json", "{ \"submodel\": \"data\" }");

        var initializer = new MockDataInitializer(_hostEnvironment, _logger);

        var ex = Assert.Throws<InternalServerException>(() => initializer.Initialize(CancellationToken.None));
        Assert.Contains(ExceptionMessages.ResourceNotValid, ex.Message, StringComparison.Ordinal);
    }
}
