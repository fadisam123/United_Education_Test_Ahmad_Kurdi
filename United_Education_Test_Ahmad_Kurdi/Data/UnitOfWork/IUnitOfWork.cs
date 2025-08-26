using United_Education_Test_Ahmad_Kurdi.Data.IRepository;

namespace United_Education_Test_Ahmad_Kurdi.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
