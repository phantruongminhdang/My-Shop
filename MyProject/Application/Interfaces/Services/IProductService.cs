using Application.Commons;
using Application.ViewModels.ProductViewModel;
using Domain.Entities;

namespace Application.Interfaces.Services
{
    public interface IProductService
    {
        public Task<Pagination<Product>> GetPagination(int pageIndex, int pageSize, bool isAdmin = false);
        public Task<Pagination<Product>> GetAll(bool isAdmin = false);
        public Task<Pagination<Product>?> GetByFilter(int pageIndex, int pageSize, FilterProductModel filterProductModel, bool isAdmin = false);
        public Task<Product?> GetById(Guid id, bool isAdmin = false);
        public Task AddAsync(ProductModel productModel);
        public Task Update(Guid id, ProductModel productModel);
        public Task Delete(Guid id);
        public Task<Pagination<Product>> GetBoughtProduct(Guid id);
        public Task<Pagination<Product>> GetByCategory(int pageIndex, int pageSize, Guid categoryId);
        public Task DisableProduct(Guid id);
        public Task<List<Product>> getCurrentCart(List<Guid> productId);
    }
}
