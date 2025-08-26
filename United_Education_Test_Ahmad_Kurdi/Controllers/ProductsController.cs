using Microsoft.AspNetCore.Mvc;
using United_Education_Test_Ahmad_Kurdi.DTOs.Category;
using United_Education_Test_Ahmad_Kurdi.DTOs.Pagination;
using United_Education_Test_Ahmad_Kurdi.DTOs.Product;
using United_Education_Test_Ahmad_Kurdi.DTOs.Response;
using United_Education_Test_Ahmad_Kurdi.Services.Products;

namespace United_Education_Test_Ahmad_Kurdi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(
            [FromRoute] Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(ApiErrorResponse.Create(
                    "Invalid product ID provided",
                    "INVALID_PRODUCT_ID",
                    GetCorrelationId()));
            }

            try
            {
                var product = await _productService.GetAsync(id);
                return Ok(ApiResponse<ProductDto>.Scucces(product, "Product retrieved successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiErrorResponse.Create(
                    $"Product with ID '{id}' was not found",
                    "PRODUCT_NOT_FOUND",
                    GetCorrelationId()));
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDto<ProductDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<PagedResultDto<ProductDto>>>> GetProducts(
            [FromQuery] PagedSortedFilteredResultRequestDto request)
        {
            // Validate query parameters
            var validationErrors = ValidateListRequest(request);
            if (validationErrors.Any())
            {
                return BadRequest(ApiErrorResponse.Create(
                    "Invalid query parameters",
                    "VALIDATION_ERROR",
                    GetCorrelationId(),
                    validationErrors));
            }

            var result = await _productService.GetListAsync(request);

            var message = result.Items.Count == 0
                ? "No products found matching the specified criteria"
                : $"Retrieved {result.Items.Count} products (page {result.Page} of {Math.Ceiling((double)result.TotalCount / (result.PageSize ?? 20))})";

            return Ok(ApiResponse<PagedResultDto<ProductDto>>.Scucces(result, message));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
            [FromBody] CreateProductDto createProductDto)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .SelectMany(x => x.Value?.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}") ?? Enumerable.Empty<string>())
                    .ToList();

                return BadRequest(ApiErrorResponse.Create(
                    "Validation failed for product creation",
                    "VALIDATION_ERROR",
                    GetCorrelationId(),
                    validationErrors));
            }

            var createdProduct = await _productService.CreateAsync(createProductDto);

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = createdProduct.Id },
                ApiResponse<ProductDto>.Scucces(createdProduct, "Product created successfully"));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProduct(
            [FromRoute] Guid id,
            [FromBody] UpdateProductDto updateProductDto)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(ApiErrorResponse.Create(
                    "Invalid product ID provided",
                    "INVALID_PRODUCT_ID",
                    GetCorrelationId()));
            }

            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .SelectMany(x => x.Value?.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}") ?? Enumerable.Empty<string>())
                    .ToList();

                return BadRequest(ApiErrorResponse.Create(
                    "Validation failed for product update",
                    "VALIDATION_ERROR",
                    GetCorrelationId(),
                    validationErrors));
            }

            try
            {
                await _productService.UpdateAsync(id, updateProductDto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiErrorResponse.Create(
                    $"Product with ID '{id}' was not found",
                    "PRODUCT_NOT_FOUND",
                    GetCorrelationId()));
            }
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct([FromRoute] Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(ApiErrorResponse.Create(
                    "Invalid product ID provided",
                    "INVALID_PRODUCT_ID",
                    GetCorrelationId()));
            }

            try
            {
                await _productService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiErrorResponse.Create(
                    $"Product with ID '{id}' was not found",
                    "PRODUCT_NOT_FOUND",
                    GetCorrelationId()));
            }
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryDto>>>> GetCategories()
        {
            var categories = await _productService.GetCategoriesAsync();
            var categoriesList = categories.ToList();

            var message = categoriesList.Count == 0
                ? "No categories found"
                : $"Retrieved {categoriesList.Count} categories";

            return Ok(ApiResponse<IEnumerable<CategoryDto>>.Scucces(categoriesList, message));
        }

        #region Private Helper Methods

        private string GetCorrelationId()
        {
            return HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        }

        private static List<string> ValidateListRequest(PagedSortedFilteredResultRequestDto request)
        {
            var errors = new List<string>();

            if (request.Page < 1)
                errors.Add("Page number must be greater than 0");

            if (request.PageSize is < 1 or > 100)
                errors.Add("Page size must be between 1 and 100");

            if (!string.IsNullOrEmpty(request.SortOrder) &&
                    !request.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
                        !request.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase))
                errors.Add("Sort order must be either 'asc' or 'desc'");

            if (!string.IsNullOrEmpty(request.SortColumn))
            {
                var validSortColumns = new[] { "name", "price", "createdat" };
                if (!validSortColumns.Contains(request.SortColumn.ToLowerInvariant()))
                    errors.Add($"Sort column must be one of: {string.Join(", ", validSortColumns)}");
            }

            return errors;
        }

        #endregion
    }
}
