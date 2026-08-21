namespace Bla.Application.Abstractions;

// What repositories return for paged queries: one page of rows plus the
// unpaged total, fetched in the same operation.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
