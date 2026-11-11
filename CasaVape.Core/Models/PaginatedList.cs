using Microsoft.EntityFrameworkCore;

namespace CasaVape.Core.Models
{
    public class PaginatedList<T> : List<T>
    {
        public const int DefaultPageSize = 12;

        public int PageIndex { get; }
        public int TotalPages { get; }
        public int TotalCount { get; }
        public int PageSize { get; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0;
            TotalCount = count;
            PageSize = pageSize;

            AddRange(items);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            if (pageSize < 1)
            {
                pageSize = DefaultPageSize;
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            var count = await source.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);

            if (totalPages > 0 && pageIndex > totalPages)
            {
                pageIndex = totalPages;
            }

            var items = await source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
