using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Extensions;

public class PagingExtensionsTests
{
    private readonly Func<TestItem, string> _idSelector;
    private readonly List<TestItem> _items;

    public PagingExtensionsTests()
    {
        _idSelector = i => i.Id;
        _items = Enumerable.Range(1, 20)
            .Select(i => new TestItem { Id = $"id{i}", Value = $"value{i}" })
            .ToList();
    }

    [Fact]
    public void GetPagedResult_ShouldReturnFirstPage_WhenNoCursorProvided()
    {
        var (pagedItems, meta) = PagingExtensions.GetPagedResult(_items, _idSelector, 5, null);

        Assert.Equal(5, pagedItems.Count);
        Assert.Equal("id1", pagedItems.First().Id);
        Assert.NotNull(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnNextPage_WhenValidCursorProvided()
    {
        var cursor = "id5".EncodeBase64Url();
        var (pagedItems, meta) = PagingExtensions.GetPagedResult(_items, _idSelector, 5, cursor);

        Assert.Equal("id6", pagedItems.First().Id);
        Assert.Equal(5, pagedItems.Count);
        Assert.NotNull(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnFromStart_WhenInvalidCursorProvided()
    {
        var cursor = "nonExistingId".EncodeBase64Url();
        var (pagedItems, meta) = PagingExtensions.GetPagedResult(_items, _idSelector, 5, cursor);

        Assert.Equal("id1", pagedItems.First().Id);
        Assert.NotNull(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnDefaultPageSize_WhenPageSizeIsNull()
    {
        var (pagedItems, meta) = PagingExtensions.GetPagedResult(_items, _idSelector, null, null);

        Assert.Equal(20, pagedItems.Count);
        Assert.Null(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldCapPageSizeAtMax_WhenPageSizeExceedsLimit()
    {
        var (pagedItems, meta) = PagingExtensions.GetPagedResult(_items, _idSelector, 2000, null);

        Assert.Equal(20, pagedItems.Count);
        Assert.Null(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnNullCursor_WhenAtEndOfList()
    {
        var cursor = "id16".EncodeBase64Url();
        var (pagedItems, meta) = PagingExtensions.GetPagedResult(_items, _idSelector, 10, cursor);

        Assert.Equal(4, pagedItems.Count);
        Assert.Null(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnNullCursor_WhenNotFullPage()
    {
        var cursor = "id18".EncodeBase64Url();
        var (pagedItems, meta) = PagingExtensions.GetPagedResult(_items, _idSelector, 5, cursor);

        Assert.Equal(2, pagedItems.Count);
        Assert.Null(meta.Cursor);
    }

    private class TestItem
    {
        public string Id { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
