using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;

public static class SubmodelsMapperProfile
{
    public static SubmodelsDto ToDto(this SubmodelList submodelList)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.MapSubmodelResponse);
        _ = activity?.SetTag("submodel.count", submodelList.Result.Count);

        return new SubmodelsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = submodelList.PagingMetaData?.Cursor
            },
            Result = new ProjectedReadOnlyList<ISubmodel, JsonObject>(submodelList.Result, Jsonization.Serialize.ToJsonObject)
        };
    }

    public static SubmodelElementsDto ToDto(this SubmodelElementsPage submodelElementsPage)
    {
        return new SubmodelElementsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = submodelElementsPage.PagingMetaData?.Cursor
            },
            Result = [.. submodelElementsPage.Result.Select(Jsonization.Serialize.ToJsonObject)]
        };
    }

    private sealed class ProjectedReadOnlyList<TSource, TResult>(IList<TSource> source, Func<TSource, TResult> selector) : IList<TResult>
    {
        public int Count => source.Count;
        public bool IsReadOnly => true;
        public TResult this[int index]
        {
            get => selector(source[index]);
            set => throw new NotSupportedException();
        }

        public IEnumerator<TResult> GetEnumerator() => source.Select(selector).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(TResult item) => this.Any(candidate => EqualityComparer<TResult>.Default.Equals(candidate, item));
        public int IndexOf(TResult item) => this.Select((candidate, index) => (candidate, index)).FirstOrDefault(pair => EqualityComparer<TResult>.Default.Equals(pair.candidate, item), (default!, -1)).index;
        public void CopyTo(TResult[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public void Add(TResult item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, TResult item) => throw new NotSupportedException();
        public bool Remove(TResult item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
    }
}
