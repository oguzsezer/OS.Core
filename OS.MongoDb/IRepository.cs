using System.Linq.Expressions;
using OS.Core.Pagination;

namespace OS.MongoDb
{
    public interface IRepository<TEntity, TResultModel, in TQuery, in TKey> 
        where TEntity : class, IEntity<TKey>, new()  
        where TResultModel : class, new()
        where TQuery : IPaginationFilter
        where TKey : IEquatable<TKey>
    {
        IQueryable<TEntity> AsQueryable();
        Task<TResultModel> GetByIdAsync(TKey id);
        Task<IPaginationResult<ICollection<TResultModel>>> GetAsync(TQuery query,  Expression<Func<TEntity, object>> sortField = null, bool desc = false);
        Task<TResultModel> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TResultModel> CreateAsync(TEntity entity);
        Task<bool> AddRangeAsync(IEnumerable<TEntity> entities);
        Task<bool> AddOrUpdateRangeAsync(IEnumerable<TEntity> entities);
        Task<bool> AddOrUpdateRangeAsync<TField>(IEnumerable<TEntity> entities, Expression<Func<TEntity, TField>> filterField);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TKey id);
        Task DeleteAsync(TEntity entity);
    }
}
