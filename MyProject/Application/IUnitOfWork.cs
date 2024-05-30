using Application.Interfaces.Repositories;

namespace Application
{
    public interface IUnitOfWork
    {
        public ICustomerRepository CustomerRepository { get; }
        public IProductRepository ProductRepository { get; }
        public ICategoryRepository CategoryRepository { get; }
        public IProductImageRepository ProductImageRepository { get; }
        public IOrderRepository OrderRepository { get; }
        public IOrderDetailRepository OrderDetailRepository { get; }
        public IOrderTransactionRepository OrderTransactionRepository { get; }
        public Task<int> SaveChangeAsync();
        void BeginTransaction();
        Task CommitTransactionAsync();
        void RollbackTransaction();
        public void ClearTrack();
    }
}
