using Application.Commons;
using Domain.Entities;
using Domain.Entities.Base;
using System.Linq.Expressions;

namespace Application.Interfaces.Repositories.Base
{
    public interface IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        Task<List<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(Guid id);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void UpdateRange(List<TEntity> entities);
        void SoftRemove(TEntity entity);
        Task AddRangeAsync(List<TEntity> entities);
        void SoftRemoveRange(List<TEntity> entities);
        IQueryable<TEntity> GetAllQueryable();

        Task<Pagination<TEntity>> GetAsync(Expression<Func<TEntity, bool>> expression = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, bool isDisableTracking = true, bool isTakeAll = false, int pageSize = 0, int pageIndex = 0, List<Expression<Func<TEntity, object>>> includes = null);
        void HardDeleteRange(List<TEntity> entities);


    }
}
