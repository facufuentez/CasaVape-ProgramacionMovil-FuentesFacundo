namespace CasaVape.Core.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public string Action { get; set; } = "Index";
        public string? Controller { get; set; }
        public Dictionary<string, string?> RouteValues { get; set; } = new();
    }
}
