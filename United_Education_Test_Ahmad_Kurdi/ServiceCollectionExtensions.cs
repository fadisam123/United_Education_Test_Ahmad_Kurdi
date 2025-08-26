using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using United_Education_Test_Ahmad_Kurdi.Data;
using United_Education_Test_Ahmad_Kurdi.Data.IRepository;
using United_Education_Test_Ahmad_Kurdi.Data.Repository;
using United_Education_Test_Ahmad_Kurdi.Data.UnitOfWork;
using United_Education_Test_Ahmad_Kurdi.Infrastructure.Cache;
using United_Education_Test_Ahmad_Kurdi.Mappers;
using United_Education_Test_Ahmad_Kurdi.Services.Products;

namespace United_Education_Test_Ahmad_Kurdi
{
    public static class ServiceCollectionExtensions
    {
        public static async Task<IServiceCollection> AddSQLServerDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure the DbContext to use SQL Server
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
            });

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            await SeedDataInDbAsync(services.BuildServiceProvider());
            return services;
        }

        private static async Task SeedDataInDbAsync(IServiceProvider services)
        {
            using (IServiceScope serviceScope = services.CreateScope())
            {
                IServiceProvider serviceProvider = serviceScope.ServiceProvider;
                var DbContext = serviceProvider.GetRequiredService<AppDbContext>();
                await DataSeeder.SeedDataAsync(DbContext);
            }
        }

        public static IServiceCollection AddMappingServices(this IServiceCollection services)
        {
            services.AddScoped<IProductMapper, ProductMapper>();
            return services;
        }

        public static IServiceCollection AddProductCatalogServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure cache settings
            services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));

            var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();

            if (cacheSettings.EnableCaching)
            {
                var redisConnectionString = configuration.GetConnectionString("Redis");

                if (!string.IsNullOrEmpty(redisConnectionString))
                {
                    try
                    {
                        var configOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
                        configOptions.ConnectTimeout = 3000;
                        configOptions.SyncTimeout = 2000;
                        configOptions.AbortOnConnectFail = false;

                        var multiplexer = ConnectionMultiplexer.Connect(configOptions);

                        if (multiplexer.IsConnected)
                        {
                            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
                            services.AddStackExchangeRedisCache(options =>
                            {
                                options.Configuration = redisConnectionString;
                                options.InstanceName = "UnitedEducationTestInstance";
                            });
                            //services.AddSingleton<ICacheHealthService, RedisCacheHealthService>();
                        }
                        else
                            throw new Exception("Error in connecting to Redis server");
                    }
                    catch (Exception ex)
                    {
                        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                        var logger = loggerFactory.CreateLogger("Startup");

                        logger.LogWarning(ex, "Failed to configure Redis, falling back to in-memory distributed cache");
                        services.AddDistributedMemoryCache();
                        //services.AddSingleton<ICacheHealthService, MemoryCacheHealthService>();
                    }
                }
            }

            services.AddScoped<IProductService, ProductService>();

            return services;
        }
    }
}
