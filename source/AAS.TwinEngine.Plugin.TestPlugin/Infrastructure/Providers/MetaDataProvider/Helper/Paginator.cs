using Aas.TwinEngine.Plugin.TestPlugin.Common.Extensions;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider.Helper;

public static class Paginator
{
    public static (IList<T> Items, PagingMetaData PagingMetaData) GetPagedResult<T>(
        IList<T> allItems,
        Func<T, string> getId,
        int? limit,
        string? cursor)
    {
        var startIndex = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            var lastId = cursor.DecodeBase64();
            startIndex = allItems.ToList().FindIndex(item => getId(item) == lastId) + 1;
        }

        var pageSize = limit ?? 100;
        var pagedItems = allItems.Skip(startIndex).Take(pageSize).ToList();

        string? nextCursor = null;

        if (limit == null && cursor == null && pagedItems.Count < pageSize)
        {
            return (pagedItems, new PagingMetaData { Cursor = nextCursor });
        }

        var lastItem = pagedItems.LastOrDefault();
        if (lastItem != null)
        {
            nextCursor = getId(lastItem).EncodeToBase64();
        }

        return (pagedItems, new PagingMetaData { Cursor = nextCursor });
    }
}
