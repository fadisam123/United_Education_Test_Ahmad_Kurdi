namespace United_Education_Test_Ahmad_Kurdi.Infrastructure.Cache
{
    public class CacheSettings
    {
        public bool EnableCaching { get; set; } = true;
        public int CacheTimeoutMs { get; set; } = 2000;
        public int SlidingExpirationMinutes { get; set; } = 10;
        public int AbsoluteExpirationMinutes { get; set; } = 60;
        public int StaleDataMaxMinutes { get; set; } = 5; // Cancellation time (the time to cancel the cache retrieval process)
        public string ProductCacheKeyPrefix { get; set; } = "product:";
        public string ProductListCacheKeyPrefix { get; set; } = "products:list:";
        public string CategoryListCacheKey { get; set; } = "categories:all";
        public string ProductListVersionKey { get; set; } = "products:list:version";
    }
}
