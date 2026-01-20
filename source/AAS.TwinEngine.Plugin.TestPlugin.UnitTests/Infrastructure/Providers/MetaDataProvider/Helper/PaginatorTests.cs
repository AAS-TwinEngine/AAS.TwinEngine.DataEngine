using Aas.TwinEngine.Plugin.TestPlugin.Common.Extensions;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider.Helper;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers.MetaDataProvider.Helper;

public class PaginatorTests
{
    private readonly Func<TestItem, string> _idSelector;
    private readonly List<TestItem> _items;

    public PaginatorTests()
    {
        _idSelector = i => i.Id;
        _items = Enumerable.Range(1, 20)
            .Select(i => new TestItem { Id = $"id{i}", Value = $"value{i}" })
            .ToList();
    }

    [Fact]
    public void GetPagedResult_ShouldReturnFirstPage_WhenNoCursorProvided()
    {
        var (pagedItems, meta) = Paginator.GetPagedResult(_items, _idSelector, 5, null);

        Assert.Equal(5, pagedItems.Count);
        Assert.Equal("id1", pagedItems.First().Id);
        Assert.NotNull(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnNextPage_WhenValidCursorProvided()
    {
        var cursor = "id5".EncodeToBase64();
        var (pagedItems, meta) = Paginator.GetPagedResult(_items, _idSelector, 5, cursor);

        Assert.Equal("id6", pagedItems.First().Id);
        Assert.Equal(5, pagedItems.Count);
        Assert.NotNull(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnFromStart_WhenInvalidCursorProvided()
    {
        var cursor = "nonExistingId".EncodeToBase64();
        var (pagedItems, meta) = Paginator.GetPagedResult(_items, _idSelector, 5, cursor);

        Assert.Equal("id1", pagedItems.First().Id);
        Assert.NotNull(meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnDefaultPageSize_WhenPageSizeIsNull()
    {
        var (pagedItems, meta) = Paginator.GetPagedResult(_items, _idSelector, null, null);

        Assert.Equal(20, pagedItems.Count);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnDefaultPageSize_WhenTotalItemIsGreaterThenPageSize_WithCursorAsLastItem()
    {
        var items = Enumerable.Range(1, 200)
                                      .Select(i => new TestItem { Id = $"id{i}", Value = $"value{i}" })
                                      .ToList();
        var expectedCursor = "id100".EncodeToBase64();

        var (pagedItems, meta) = Paginator.GetPagedResult(items, _idSelector, null, null);

        Assert.Equal(100, pagedItems.Count);
        Assert.Equal(expectedCursor, meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldCapPageSizeAtMax_WhenPageSizeExceedsLimit()
    {
        var expectedCursor = "id20".EncodeToBase64();

        var (pagedItems, meta) = Paginator.GetPagedResult(_items, _idSelector, 2000, null);

        Assert.Equal(20, pagedItems.Count);
        Assert.Equal(expectedCursor, meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldUseZeroPageSize_WhenPageSizeIsZeroOrNegative()
    {
        var (pagedItems1, _) = Paginator.GetPagedResult(_items, _idSelector, 0, null);
        var (pagedItems2, _) = Paginator.GetPagedResult(_items, _idSelector, -10, null);

        Assert.Equal(0, pagedItems1.Count);
        Assert.Equal(0, pagedItems2.Count);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnLastIdAsCursor_WhenAtEndOfList()
    {
        var cursor = "id16".EncodeToBase64();
        var expectedCursor = "id20".EncodeToBase64();
        var (pagedItems, meta) = Paginator.GetPagedResult(_items, _idSelector, 10, cursor);

        Assert.Equal(4, pagedItems.Count);
        Assert.Equal(expectedCursor, meta.Cursor);
    }

    [Fact]
    public void GetPagedResult_ShouldReturnLastIdAsCursor_WhenNotFullPage()
    {
        var cursor = "id18".EncodeToBase64();
        var expectedCursor = "id20".EncodeToBase64();
        var (pagedItems, meta) = Paginator.GetPagedResult(_items, _idSelector, 5, cursor);

        Assert.Equal(2, pagedItems.Count);
        Assert.Equal(expectedCursor, meta.Cursor);
    }

    private class TestItem
    {
        public string Id { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}

