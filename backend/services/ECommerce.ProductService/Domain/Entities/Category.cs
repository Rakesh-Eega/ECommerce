namespace ECommerce.ProductService.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string? ImageUrl { get; private set; }
        public Guid? ParentId { get; private set; }
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public Category? Parent { get; private set; }
        public List<Category> Children { get; private set; } = new();
        public List<Product> Products { get; private set; } = new();

        private Category() { }

        public static Category Create(string name, string slug, Guid? parentId = null)
            => new() { Name = name, Slug = slug.ToLowerInvariant(), ParentId = parentId };

        public void Update(string name, string slug) { Name = name; Slug = slug; }
        public void Deactivate() => IsActive = false;
    }
}
