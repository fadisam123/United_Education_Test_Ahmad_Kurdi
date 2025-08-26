
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using United_Education_Test_Ahmad_Kurdi.Data.UnitOfWork;
using United_Education_Test_Ahmad_Kurdi.Domain.Models;
using United_Education_Test_Ahmad_Kurdi.DTOs.Category;
using United_Education_Test_Ahmad_Kurdi.DTOs.Pagination;
using United_Education_Test_Ahmad_Kurdi.DTOs.Product;
using United_Education_Test_Ahmad_Kurdi.Infrastructure.Cache;
using United_Education_Test_Ahmad_Kurdi.Mappers;

namespace United_Education_Test_Ahmad_Kurdi.Services.Products
{
    public class ProductService : IProductService
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ProductService> _logger;
        private readonly CacheSettings _cacheSettings;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        #endregion

        #region Constructor
        public ProductService(
            IUnitOfWork unitOfWork,
            IProductMapper mapper,
            IDistributedCache cache,
            ILogger<ProductService> logger,
            IOptions<CacheSettings> cacheSettings)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
        }
        #endregion

        #region Product Service Method
        public async Task<ProductDto> GetAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Product ID cannot be empty", nameof(id));
            }

            var cacheKey = $"{_cacheSettings.ProductCacheKeyPrefix}{id}";
            var semaphore = _semaphores.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            // Try to get from cache first
            var cachedData = await TryGetFromCacheAsync<ProductDto>(cacheKey);
            if (cachedData != null)
                return cachedData; // Cache HIT

            // Cache MISS fetch from database
            // Use semaphore to prevent cache stampede
            await semaphore.WaitAsync();
            try
            {
                var product = await _unitOfWork.Products.GetByIdWithCategoryAsync(id);
                if (product == null)
                {
                    throw new KeyNotFoundException($"Product with ID {id} not found");
                }

                var productDto = _mapper.ToProductDto(product);

                // Cache the result
                await TrySetToCacheAsync(cacheKey, productDto);

                return productDto;
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(ex, "Database error while fetching product {ProductId}", id);

                // Do something here as stated in the test requirements
                // (e.g., Try to serve stale cache if available),
                // but I do not because there is a contradiction
                // in the requirements (think about timing).

                throw;
            }
            finally
            {
                // release the semaphore that we awaited earlier
                semaphore.Release();

                // Cleanup if possible (to avoid unbounded growth).
                try
                {
                    if (semaphore.CurrentCount == 1) // no one is currently inside the semaphore
                    {
                        // Only remove if the dictionary still maps to the same SemaphoreSlim instance
                        // to avoid removing a newly-created semaphore (race).
                        if (_semaphores.TryGetValue(cacheKey, out var existing) &&
                            ReferenceEquals(existing, semaphore))
                        {
                            _semaphores.TryRemove(cacheKey, out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Don't crash production for cleanup problems; log only.
                    _logger.LogDebug(ex, "Semaphore cleanup failed for {CacheKey}", cacheKey);
                }
            }
        }

        public async Task<PagedResultDto<ProductDto>> GetListAsync(PagedSortedFilteredResultRequestDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var cacheKey = await GenerateListCacheKeyAsync(input);
            var semaphore = _semaphores.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            // Try to get from cache first
            var cachedData = await TryGetFromCacheAsync<PagedResultDto<ProductDto>>(cacheKey);
            if (cachedData != null)
                return cachedData; // Cache HIT


            // Cache MISS fetch from database
            // Use semaphore to prevent cache stampede
            await semaphore.WaitAsync();
            try
            {
                var filter = BuildFilter(input.Filter);
                var orderBy = BuildOrderBy(input.SortColumn, input.SortOrder);

                var pageSize = input.PageSize ?? 20;
                var skip = (input.Page - 1) * pageSize;

                var products = await _unitOfWork.Products.GetAllWithCategoriesAsync(
                    filter: filter,
                    orderBy: orderBy,
                    take: pageSize,
                    skip: skip);

                var totalCount = await _unitOfWork.Products.CountWithFilterAsync(filter);

                var productDtos = products.Select(_mapper.ToProductDto).ToList();
                var result = new PagedResultDto<ProductDto>(productDtos, totalCount, input.Page, pageSize);

                // Cache the result
                await TrySetToCacheAsync(cacheKey, result);

                return result;
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(ex, "Database error while fetching products");

                // Do something here as stated in the test requirements
                // (e.g., Try to serve stale cache if available),
                // but I do not because there is a contradiction
                // in the requirements (think about timing).

                throw;
            }
            finally
            {
                // release the semaphore that we awaited earlier
                semaphore.Release();

                // Cleanup if possible (to avoid unbounded growth).
                try
                {
                    if (semaphore.CurrentCount == 1) // no one is currently inside the semaphore
                    {
                        // Only remove if the dictionary still maps to the same SemaphoreSlim instance
                        // to avoid removing a newly-created semaphore (race).
                        if (_semaphores.TryGetValue(cacheKey, out var existing) &&
                            ReferenceEquals(existing, semaphore))
                        {
                            _semaphores.TryRemove(cacheKey, out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Don't crash production for cleanup problems; log only.
                    _logger.LogDebug(ex, "Semaphore cleanup failed for {CacheKey}", cacheKey);
                }
            }
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var product = _mapper.ToProduct(input);

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate caches after successful creation
            await InvalidateProductCaches();

            // Load with category for return
            var createdProduct = await _unitOfWork.Products.GetByIdWithCategoryAsync(product.Id);
            if (createdProduct == null)
            {
                throw new InvalidOperationException("Failed to retrieve created product");
            }

            return _mapper.ToProductDto(createdProduct);
        }

        public async Task UpdateAsync(Guid id, UpdateProductDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (id == Guid.Empty)
            {
                throw new ArgumentException("Product ID cannot be empty", nameof(id));
            }

            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {id} not found");
            }

            _mapper.ToProduct(input, product);
            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate caches after successful update
            await InvalidateProductCaches();
            await InvalidateSpecificProductCache(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Product ID cannot be empty", nameof(id));
            }

            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {id} not found");
            }

            await _unitOfWork.Products.RemoveAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate caches after successful deletion
            await InvalidateProductCaches();
            await InvalidateSpecificProductCache(id);
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            var cacheKey = _cacheSettings.CategoryListCacheKey;
            var semaphore = _semaphores.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            // Try to get from cache first
            var cachedData = await TryGetFromCacheAsync<IEnumerable<CategoryDto>>(cacheKey);
            if (cachedData != null)
                return cachedData; // Cache HIT

            // Cache MISS fetch from database
            // Use semaphore to prevent cache stampede
            await semaphore.WaitAsync();
            try
            {
                var categories = await _unitOfWork.Categories.GetAllAsync();
                var categoryDtos = categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name });

                // Cache the result
                await TrySetToCacheAsync(cacheKey, categoryDtos);

                return categoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while fetching categories");

                // Do something here as stated in the test requirements
                // (e.g., Try to serve stale cache if available),
                // but I do not because there is a contradiction
                // in the requirements (think about timing).

                throw;
            }
            finally
            {
                // release the semaphore that we awaited earlier
                semaphore.Release();

                // Cleanup if possible (to avoid unbounded growth).
                try
                {
                    if (semaphore.CurrentCount == 1) // no one is currently inside the semaphore
                    {
                        // Only remove if the dictionary still maps to the same SemaphoreSlim instance
                        // to avoid removing a newly-created semaphore (race).
                        if (_semaphores.TryGetValue(cacheKey, out var existing) &&
                            ReferenceEquals(existing, semaphore))
                        {
                            _semaphores.TryRemove(cacheKey, out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Don't crash production for cleanup problems; log only.
                    _logger.LogDebug(ex, "Semaphore cleanup failed for cacheKey={CacheKey}", cacheKey);
                }
            }
        }
        #endregion

        #region Helper Methods
        private static Expression<Func<Product, bool>>? BuildFilter(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return null;

            return p => p.Name.Contains(filter) ||
                       (p.Description != null && p.Description.Contains(filter)) ||
                       (p.Category != null && p.Category.Name.Contains(filter));
        }

        private static Func<IQueryable<Product>, IOrderedQueryable<Product>> BuildOrderBy(string? sortColumn, string sortOrder)
        {
            return sortColumn?.ToLower() switch
            {
                "name" => sortOrder.ToLower() == "asc"
                    ? query => query.OrderBy(p => p.Name)
                    : query => query.OrderByDescending(p => p.Name),
                "price" => sortOrder.ToLower() == "asc"
                    ? query => query.OrderBy(p => p.Price)
                    : query => query.OrderByDescending(p => p.Price),
                "createdat" => sortOrder.ToLower() == "asc"
                    ? query => query.OrderBy(p => p.CreatedAt)
                    : query => query.OrderByDescending(p => p.CreatedAt),
                _ => query => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        #endregion

        #region Cache Related Method
        private async Task<T?> TryGetFromCacheAsync<T>(string key) where T : class
        {
            if (_cacheSettings.EnableCaching)
            {
                try
                {
                    using var cts = new CancellationTokenSource(_cacheSettings.CacheTimeoutMs);
                    var cachedValue = await _cache.GetStringAsync(key, cts.Token);

                    if (!string.IsNullOrEmpty(cachedValue))
                    {
                        _logger.LogDebug("Cache HIT for {CacheKey}", key);
                        var result = JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
                        return result;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Cache read timeout for {CacheKey}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Cache read error for {CacheKey}", key);
                }
            }
            _logger.LogDebug("Cache MISS for {CacheKey}", key);
            return null;
        }

        private async Task TrySetToCacheAsync<T>(string key, T value) where T : class
        {
            if (!_cacheSettings.EnableCaching)
                return;

            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_cacheSettings.SlidingExpirationMinutes),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheSettings.AbsoluteExpirationMinutes)
                };

                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);

                using var cts = new CancellationTokenSource(_cacheSettings.CacheTimeoutMs);
                await _cache.SetStringAsync(key, serializedValue, options, cts.Token);

                _logger.LogDebug("{CacheKey} cached successfully", key);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Cache write timeout for  {CacheKey}", key);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cache write error {CacheKey}", key);
            }
        }

        private async Task<string> GenerateListCacheKeyAsync(PagedSortedFilteredResultRequestDto input)
        {
            // Try to read version from cache (if not present default to "0")
            string version = "0";
            try
            {
                if (_cacheSettings.EnableCaching)
                {
                    using var cts = new CancellationTokenSource(_cacheSettings.CacheTimeoutMs);
                    var versionValue = await _cache.GetStringAsync(_cacheSettings.ProductListVersionKey, cts.Token);
                    if (!string.IsNullOrEmpty(versionValue))
                        version = versionValue;
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with default version
                _logger.LogDebug(ex, "Failed to read cache version, using default");
            }

            // Normalize input values to ensure consistent keys
            var normalizedFilter = string.IsNullOrWhiteSpace(input.Filter) ? "null" : input.Filter.Trim().ToLowerInvariant();
            var normalizedSortColumn = string.IsNullOrWhiteSpace(input.SortColumn) ? "null" : input.SortColumn.Trim().ToLowerInvariant();
            var normalizedSortOrder = input.SortOrder?.ToLowerInvariant() ?? "desc";
            var pageSize = input.PageSize ?? 10;

            var keyParts = new[]
            {
                _cacheSettings.ProductListCacheKeyPrefix,
                version,
                normalizedFilter,
                normalizedSortColumn,
                normalizedSortOrder,
                input.Page.ToString(),
                pageSize.ToString()
            };

            return string.Join(":", keyParts);
        }

        private async Task InvalidateProductCaches()
        {
            if (!_cacheSettings.EnableCaching)
                return;

            try
            {
                using var cts = new CancellationTokenSource(_cacheSettings.CacheTimeoutMs);

                // Bump the list-version token (set to a new GUID) so list cache keys change
                try
                {
                    // Options for Product Lis Version Key (not necessary from _cacheSettings)
                    var options = new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromHours(1),
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    };

                    await _cache.SetStringAsync(_cacheSettings.ProductListVersionKey, Guid.NewGuid().ToString("N"), options, cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to update product list version token during invalidation");
                }

                _logger.LogDebug("Invalidated product-related caches (list version bumped)");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Cache invalidation timeout for product caches");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error invalidating product caches");
            }
        }

        private async Task InvalidateSpecificProductCache(Guid productId)
        {
            if (!_cacheSettings.EnableCaching)
                return;

            var cacheKey = $"{_cacheSettings.ProductCacheKeyPrefix}{productId}";
            try
            {
                using var cts = new CancellationTokenSource(_cacheSettings.CacheTimeoutMs);
                await _cache.RemoveAsync(cacheKey, cts.Token);
                _logger.LogDebug("Invalidated cache for {ProductId}", productId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Cache invalidation timeout for {ProductId}", productId);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error invalidating cache for {ProductId}", productId);
            }
        }
        #endregion
    }
}
