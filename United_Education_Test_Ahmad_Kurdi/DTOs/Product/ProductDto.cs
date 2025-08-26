using United_Education_Test_Ahmad_Kurdi.DTOs.Category;

namespace United_Education_Test_Ahmad_Kurdi.DTOs.Product
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public float Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; } = null;

        public CategoryDto? Category { get; set; }
    }
}
