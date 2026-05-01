// Infrastructure/Search/ElasticsearchService.cs
using Nest;
using ECommerce.ProductService.Application.DTOs;

namespace ECommerce.ProductService.Infrastructure.Search;

public interface IElasticsearchService
{
    Task IndexProductAsync(ProductDocument document);
    Task UpdateProductAsync(ProductDocument document);
    Task DeleteProductAsync(string productId);
    Task<SearchResultDto> SearchAsync(ProductSearchQuery query);
}

public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private const string IndexName = "products";

    public ElasticsearchService(IElasticClient client)
    {
        _client = client;
        EnsureIndexExists().GetAwaiter().GetResult();
    }

    private async Task EnsureIndexExists()
    {
        var exists = await _client.Indices.ExistsAsync(IndexName);
        if (exists.Exists) return;

        await _client.Indices.CreateAsync(IndexName, c => c
            .Map<ProductDocument>(m => m
                .AutoMap()
                .Properties(p => p
                    .Text(t => t.Name(n => n.Name).Analyzer("standard"))
                    .Text(t => t.Name(n => n.Description).Analyzer("standard"))
                    .Text(t => t.Name(n => n.Slug).Analyzer("standard"))
                    .Keyword(k => k.Name(n => n.Brand))
                    .Keyword(k => k.Name(n => n.Category))
                    .Number(n => n.Name(nn => nn.MinPrice).Type(NumberType.Double))
                    .Number(n => n.Name(nn => nn.Rating).Type(NumberType.Double))
                )
            )
        );
    }

    public async Task IndexProductAsync(ProductDocument document)
    {
        await _client.IndexAsync(document, i => i
            .Index(IndexName)
            .Id(document.Id));
    }

    public async Task UpdateProductAsync(ProductDocument document)
    {
        await _client.UpdateAsync<ProductDocument>(document.Id, u => u
            .Index(IndexName)
            .Doc(document));
    }

    public async Task DeleteProductAsync(string productId)
    {
        await _client.DeleteAsync<ProductDocument>(productId,
            d => d.Index(IndexName));
    }

    public async Task<SearchResultDto> SearchAsync(ProductSearchQuery query)
    {
        var response = await _client.SearchAsync<ProductDocument>(s => s
            .Index(IndexName)
            .From((query.Page - 1) * query.PageSize)
            .Size(query.PageSize)
            .Query(q =>
            {
                QueryContainer container = q.Bool(b =>
                {
                    // Full-text search
                    var mustClauses = new List<QueryContainer>();
                    if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                    {
                        mustClauses.Add(q.MultiMatch(mm => mm
    .Fields(f => f
        .Field(p => p.Name, boost: 3)
        .Field(p => p.Brand, boost: 2)
        .Field(p => p.Description)
        .Field(p => p.Slug, boost: 1)) // ← add slug
    .Query(query.SearchTerm)
    .Type(TextQueryType.BestFields)
    .Fuzziness(Fuzziness.Auto)
));
                    }

                    // Always filter to active + approved
                    var filterClauses = new List<QueryContainer>
                    {
                        q.Term(t => t.Field(p => p.IsActive).Value(true)),
                        q.Term(t => t.Field(p => p.IsApproved).Value(true))
                    };

                    // Category filter
                    if (!string.IsNullOrWhiteSpace(query.CategoryId))
                        filterClauses.Add(q.Term(t => t
                            .Field(p => p.CategoryId).Value(query.CategoryId)));

                    // Brand filter
                    if (query.Brands?.Any() == true)
                        filterClauses.Add(q.Terms(t => t
                            .Field(p => p.Brand).Terms(query.Brands)));

                    // Price range
                    if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
                        filterClauses.Add(q.Range(r =>
                        {
                            r = r.Field(p => p.MinPrice);
                            if (query.MinPrice.HasValue) r = r.GreaterThanOrEquals((double)query.MinPrice.Value);
                            if (query.MaxPrice.HasValue) r = r.LessThanOrEquals((double)query.MaxPrice.Value);
                            return r;
                        }));

                    // In stock filter
                    if (query.InStockOnly)
                        filterClauses.Add(q.Range(r => r
                            .Field(p => p.TotalStock).GreaterThan(0)));

                    return b
                        .Must(mustClauses.ToArray())
                        .Filter(filterClauses.ToArray());
                });

                return container;
            })
            .Sort(ss =>
            {
                return query.SortBy switch
                {
                    "price_asc" => ss.Ascending(p => p.MinPrice),
                    "price_desc" => ss.Descending(p => p.MinPrice),
                    "rating" => ss.Descending(p => p.Rating),
                    "newest" => ss.Descending(p => p.CreatedAt),
                    _ => ss.Descending(p => p.Rating) // default: popularity
                };
            })
            .Aggregations(a => a
                .Terms("brands", t => t.Field(p => p.Brand).Size(20))
                .Terms("categories", t => t.Field(p => p.Category).Size(20))
                .Stats("price_stats", st => st.Field(p => p.MinPrice))
            )
        );

        var brands = response.Aggregations.Terms("brands")
            .Buckets.Select(b => new FacetItem(b.Key, b.DocCount ?? 0))
            .ToList();

        var priceStats = response.Aggregations.Stats("price_stats");

        return new SearchResultDto(
            Items: response.Documents.Select(MapToDto).ToList(),
            Total: (int)response.Total,
            Page: query.Page,
            PageSize: query.PageSize,
            Brands: brands,
            PriceMin: (decimal)(priceStats.Min ?? 0),
            PriceMax: (decimal)(priceStats.Max ?? 0)
        );
    }

    private static ProductSummaryDto MapToDto(ProductDocument doc) => new(
        Id: doc.Id,
        Name: doc.Name,
        Slug: doc.Slug,
        Brand: doc.Brand,
        Category: doc.Category,
        MinPrice: doc.MinPrice,
        MaxPrice: doc.MaxPrice,
        Rating: doc.Rating,
        ReviewCount: doc.ReviewCount,
        PrimaryImage: doc.PrimaryImage,
        InStock: doc.TotalStock > 0
    );
}