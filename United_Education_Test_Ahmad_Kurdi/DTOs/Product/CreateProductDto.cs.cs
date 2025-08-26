using System.ComponentModel.DataAnnotations;

namespace United_Education_Test_Ahmad_Kurdi.DTOs.Product
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product Name is required.")]
        [StringLength(64, ErrorMessage = "Product Name cannot exceed 64 characters.")]
        public string Name { get; set; } = null!;
        [StringLength(1024, ErrorMessage = "The Description cannot exceed 1024 characters.")]
        public string? Description { get; set; }
        [Range(0.001, float.MaxValue, ErrorMessage = "Price must be grater than zero")]
        public float Price { get; set; }

        public Guid? CategoryId { get; set; }
    }
}
