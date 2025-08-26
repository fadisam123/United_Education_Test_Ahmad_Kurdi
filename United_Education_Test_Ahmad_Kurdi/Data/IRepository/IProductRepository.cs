using System.Linq.Expressions;
using United_Education_Test_Ahmad_Kurdi.Domain.Models;

namespace United_Education_Test_Ahmad_Kurdi.Data.IRepository
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetByIdWithCategoryAsync(Guid id);
        Task<IEnumerable<Product>> GetAllWithCategoriesAsync(Expression<Func<Product, bool>>? filter = null,
            Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null,
            int? take = null, int? skip = null);
        Task<int> CountWithFilterAsync(Expression<Func<Product, bool>>? filter = null);
    }
}
