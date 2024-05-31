using Application.Commons;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Infrastructures.Repositories.Base
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        protected DbSet<TEntity> _dbSet;
        private readonly ICurrentTime _timeService;
        private readonly IClaimsService _claimsService;
        private readonly AppDbContext _context;

        public GenericRepository(AppDbContext context, ICurrentTime timeService, IClaimsService claimsService)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
            _timeService = timeService;
            _claimsService = claimsService;
        }
        public Task<List<TEntity>> GetAllAsync() => _dbSet.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync();

        public IQueryable<TEntity> GetAllQueryable() => _dbSet.Where(x => !x.IsDeleted).AsNoTracking();


        public async Task<TEntity?> GetByIdAsync(Guid id)
        {
            var result = await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            // todo should throw exception when not found
            return result;
        }
        public async Task AddAsync(TEntity entity)
        {
            entity.CreationDate = _timeService.GetCurrentTime();
            entity.CreatedBy = _claimsService.GetCurrentUserId;
            await _dbSet.AddAsync(entity);
        }

        public void SoftRemove(TEntity entity)
        {
            entity.DeletionDate = _timeService.GetCurrentTime();
            entity.IsDeleted = true;
            entity.DeleteBy = _claimsService.GetCurrentUserId;
            _dbSet.Update(entity);
        }

        public void Update(TEntity entity)
        {
            entity.ModificationDate = _timeService.GetCurrentTime();
            entity.ModificationBy = _claimsService.GetCurrentUserId;
            _dbSet.Update(entity);
        }

        public async Task AddRangeAsync(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                entity.CreationDate = _timeService.GetCurrentTime();
                entity.CreatedBy = _claimsService.GetCurrentUserId;
            }
            await _dbSet.AddRangeAsync(entities);
        }

        public void SoftRemoveRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.DeletionDate = _timeService.GetCurrentTime();
                entity.DeleteBy = _claimsService.GetCurrentUserId;
            }
            _dbSet.UpdateRange(entities);
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> expression = null)
        {
            return expression == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(expression);
        }
        public async Task<Pagination<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> expression = null, 
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, 
            bool isDisableTracking = true, 
            bool isTakeAll = false, 
            int pageSize = 0, 
            int pageIndex = 0, 
            List<Expression<Func<TEntity, object>>> includes = null)
        {
            IQueryable<TEntity> query = _dbSet;
            var paginationResult = new Pagination<TEntity>();
            paginationResult.PageIndex = pageIndex;
            if (pageSize == 0)
                paginationResult.PageSize = await CountAsync(expression);
            else
                paginationResult.PageSize = pageSize;
            paginationResult.TotalItemsCount = await CountAsync(expression);
            if (includes != null && includes.Any())
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            if (expression != null)
                query = query.Where(expression);
            if (isDisableTracking is true)
                query = query.AsNoTracking();
            if (isTakeAll is true)

            {
                if (orderBy != null)
                    paginationResult.Items = await orderBy(query).ToListAsync();
                else
                    paginationResult.Items = await query.ToListAsync();
            }
            else
            {
                if (pageIndex < 0 || pageSize < 0)
                {
                    throw new Exception("Số trang và sô lượng trong trang phải lớn hơn 0.");
                }
                if (orderBy == null)
                    paginationResult.Items = await query.Skip(pageSize * pageIndex).Take(pageSize).ToListAsync();
                else
                    paginationResult.Items = await orderBy(query).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
            }
            return paginationResult;
        }

        public void UpdateRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                entity.CreationDate = _timeService.GetCurrentTime();
                entity.CreatedBy = _claimsService.GetCurrentUserId;
            }
            _dbSet.UpdateRange(entities);
        }

        public void HardDeleteRange(List<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
        }
    }
}
