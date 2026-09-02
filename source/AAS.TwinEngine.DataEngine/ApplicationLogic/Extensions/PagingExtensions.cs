using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

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
            var lastIndex = -1;

            for (var i = 0; i < allItems.Count; i++)
            {
                if (getId(allItems[i]) == lastId)
                {
                    lastIndex = i;
                    break;
                }
            }

            if (lastIndex < 0)
            {
                throw new BadRequestException("The provided cursor does not reference an item in the collection.");
            }

            startIndex = lastIndex + 1;
        }

        var pageSize = limit ?? int.MaxValue;
        var pagedItems = allItems.Skip(startIndex).Take(pageSize).ToList();

        if (pagedItems.Count < pageSize || startIndex + pagedItems.Count >= allItems.Count)
        {
            return (pagedItems, new PagingMetaData { Cursor = null });
        }

        var nextCursor = getId(pagedItems[^1]).EncodeBase64Url();

        return (pagedItems, new PagingMetaData { Cursor = nextCursor });
    }
}
