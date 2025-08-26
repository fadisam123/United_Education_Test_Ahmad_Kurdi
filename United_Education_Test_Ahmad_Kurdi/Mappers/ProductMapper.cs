using United_Education_Test_Ahmad_Kurdi.Domain.Models;
using United_Education_Test_Ahmad_Kurdi.DTOs.Category;
using United_Education_Test_Ahmad_Kurdi.DTOs.Product;

namespace United_Education_Test_Ahmad_Kurdi.Mappers
{
    public class ProductMapper : IProductMapper
    {
        public ProductDto ToProductDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CreatedAt = product.CreatedAt,
                LastUpdated = product.LastUpdated,
                Category = product.Category != null ? new CategoryDto
                {
                    Id = product.Category.Id,
                    Name = product.Category.Name
                } : null
            };
        }

        public Product ToProduct(CreateProductDto createProductDto)
        {
            return new Product
            {
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                CategoryId = createProductDto.CategoryId
            };
        }

        public void ToProduct(UpdateProductDto updateProductDto, Product product)
        {
            product.Name = updateProductDto.Name;
            product.Description = updateProductDto.Description;
            product.Price = updateProductDto.Price;
            product.CategoryId = updateProductDto.CategoryId;

            product.LastUpdated = DateTime.UtcNow;
        }
    }
}
