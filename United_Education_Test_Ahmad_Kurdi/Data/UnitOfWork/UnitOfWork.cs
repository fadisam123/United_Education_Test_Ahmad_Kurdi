using Microsoft.EntityFrameworkCore.Storage;
using United_Education_Test_Ahmad_Kurdi.Data.IRepository;
using United_Education_Test_Ahmad_Kurdi.Data.Repository;

namespace United_Education_Test_Ahmad_Kurdi.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        public IProductRepository Products { get; }
        public ICategoryRepository Categories { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            Products = new ProductRepository(_context);
            Categories = new CategoryRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction in progress.");

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
                return;

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();

            await _context.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
