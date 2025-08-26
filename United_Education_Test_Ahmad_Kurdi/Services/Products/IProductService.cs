using United_Education_Test_Ahmad_Kurdi.DTOs.Category;
using United_Education_Test_Ahmad_Kurdi.DTOs.Pagination;
using United_Education_Test_Ahmad_Kurdi.DTOs.Product;

namespace United_Education_Test_Ahmad_Kurdi.Services.Products
{
    public interface IProductService
    {
        Task<ProductDto> GetAsync(Guid id);

        Task<PagedResultDto<ProductDto>> GetListAsync(PagedSortedFilteredResultRequestDto input);

        Task<ProductDto> CreateAsync(CreateProductDto input);

        Task UpdateAsync(Guid id, UpdateProductDto input);

        Task DeleteAsync(Guid id);

        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    }
}
