using Microsoft.AspNetCore.Mvc;

namespace ECommerce.ProductService.Application.DTOs
{
    public class ProductSearchQuery
    {
        public string? SearchTerm { get; set; }
        public string? CategoryId { get; set; }
        [FromQuery(Name = "brands")]
        public List<string>? Brands { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; } = false;
        public string SortBy { get; set; } = "popularity";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
