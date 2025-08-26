using Microsoft.EntityFrameworkCore;
using United_Education_Test_Ahmad_Kurdi.Domain.Categories;
using United_Education_Test_Ahmad_Kurdi.Domain.Models;

namespace United_Education_Test_Ahmad_Kurdi.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(AppDbContext dbContext)
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await dbContext.Database.MigrateAsync();
            }


            await SeedCategoriesAsync(dbContext);
            await SeedProductsAsync(dbContext);

        }

        private static async Task SeedCategoriesAsync(AppDbContext dbContext)
        {
            if (!dbContext.Categories.Any())
            {
                var Categories = new List<Category>
                    {
                        new Category { Name = "Gardening Tools" },
                        new Category { Name = "Planters & Pots" },
                        new Category { Name = "Seeds & Fertilizers" },
                        new Category { Name = "Accessories" },

                    };
                await dbContext.Categories.AddRangeAsync(Categories);
                await dbContext.SaveChangesAsync();
            }
        }

        private static async Task SeedProductsAsync(AppDbContext dbContext)
        {
            if (!dbContext.Products.Any())
            {
                var products = new List<Product>
                    {
                        new Product { Name = "EcoSpade™ - Bamboo Garden Spade", Description = "A durable, lightweight spade made from sustainably sourced bamboo with a rust-resistant stainless steel blade. Perfect for digging, planting, and weeding", Price = 15f, Category = dbContext.Categories.First(a => a.Name == "Gardening Tools")},
                        new Product { Name = "CompostMate™ - Compost Bin Starter Kit", Description = "An easy-to-use compost bin kit that includes a 5-gallon container, carbon filters to reduce odors, and a guidebook for beginners", Price = 29.99f, Category = dbContext.Categories.First(a => a.Name == "Gardening Tools")},
                        new Product { Name = "GardenGrip™ - Ergonomic Pruning Shears", Price = 40f, Category = dbContext.Categories.First(a => a.Name == "Gardening Tools")},

                        new Product { Name = "HangingHaven™ - Macramé Plant Hanger", Description = "Handcrafted macramé hangers made from organic cotton. Ideal for suspending small to medium-sized pots indoors or outdoors", Price = 40f, Category = dbContext.Categories.First(a => a.Name == "Planters & Pots")},

                        new Product { Name = "SeedBox™ - Herb Garden Starter Kit", Description = "Includes packets of basil, mint, cilantro, and parsley seeds, along with biodegradable starter pots and nutrient-rich soil discs", Price = 5f, Category = dbContext.Categories.First(a => a.Name == "Seeds & Fertilizers")},
                        new Product { Name = "BioBoost™ - Organic Liquid Fertilizer (1L)", Description = "Made from seaweed extract and fish emulsion, this all-natural fertilizer promotes healthy growth and vibrant blooms", Price = 17.99f, Category = dbContext.Categories.First(a => a.Name == "Seeds & Fertilizers")},
                        new Product { Name = "Product Without Category", Description = "Some description", Price = 8f },
                    };
                await dbContext.Products.AddRangeAsync(products);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
