using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

public static class PagingExtensions
{
    public static (IList<T> Items, PagingMetaData PagingMetaData) GetPagedResult<T>(
        IList<T> allItems,
        Func<T, string> getId,
        int? limit,
        string? cursor) where T : class
    {
        var startIndex = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            var lastId = cursor.DecodeBase64Url();
            startIndex = allItems.ToList().FindIndex(item => getId(item) == lastId) + 1;
        }

        var pageSize = limit ?? 100;
        var pagedItems = allItems.Skip(startIndex).Take(pageSize).ToList();

        string? nextCursor = null;
        if (pagedItems.Count != pageSize || (startIndex + pageSize) >= allItems!.Count)
        {
            return (pagedItems, new PagingMetaData { Cursor = nextCursor });
        }

        var lastItem = pagedItems.LastOrDefault();
        if (lastItem != null)
        {
            nextCursor = getId(lastItem).EncodeBase64Url();
        }

        return (pagedItems, new PagingMetaData { Cursor = nextCursor });
    }
}
