using United_Education_Test_Ahmad_Kurdi.Domain.Models;
using United_Education_Test_Ahmad_Kurdi.DTOs.Product;

namespace United_Education_Test_Ahmad_Kurdi.Mappers
{
    public interface IProductMapper
    {
        // Maps a Product entity to a ProductDto
        ProductDto ToProductDto(Product product);

        // Maps a CreateProductDto to a new Product entity
        Product ToProduct(CreateProductDto createProductDto);

        // Applies updates from an UpdateProductDto to an existing Product entity
        void ToProduct(UpdateProductDto updateProductDto, Product product);
    }
}
