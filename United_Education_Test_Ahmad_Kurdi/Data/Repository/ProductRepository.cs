using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using United_Education_Test_Ahmad_Kurdi.Data.IRepository;
using United_Education_Test_Ahmad_Kurdi.Domain.Models;

namespace United_Education_Test_Ahmad_Kurdi.Data.Repository
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Product?> GetByIdWithCategoryAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> GetAllWithCategoriesAsync(
            Expression<Func<Product, bool>>? filter = null,
            Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null,
            int? take = null,
            int? skip = null)
        {
            IQueryable<Product> query = _dbSet.Include(p => p.Category);

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = orderBy(query);

            if (skip.HasValue)
                query = query.Skip(skip.Value);

            if (take.HasValue)
                query = query.Take(take.Value);

            return await query.ToListAsync();
        }

        public async Task<int> CountWithFilterAsync(Expression<Func<Product, bool>>? filter = null)
        {
            IQueryable<Product> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            return await query.CountAsync();
        }
    }
}
