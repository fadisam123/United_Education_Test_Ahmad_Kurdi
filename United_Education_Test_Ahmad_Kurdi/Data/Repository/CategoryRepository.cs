using United_Education_Test_Ahmad_Kurdi.Data.IRepository;
using United_Education_Test_Ahmad_Kurdi.Domain.Categories;

namespace United_Education_Test_Ahmad_Kurdi.Data.Repository
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}
