namespace ParkDesk.Api.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagedResultExtensions
{
    public static PagedResult<TOut> Map<TIn, TOut>(this PagedResult<TIn> source, Func<TIn, TOut> selector) => new()
    {
        Items = source.Items.Select(selector).ToList(),
        Page = source.Page,
        PageSize = source.PageSize,
        TotalCount = source.TotalCount
    };
}
