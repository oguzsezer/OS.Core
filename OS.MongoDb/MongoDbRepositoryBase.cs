using System.Linq.Expressions;
using System.Reflection;
using Mapster;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OS.Core.Pagination;

namespace OS.MongoDb
{
    public abstract class MongoDbRepositoryBase<TEntity, TResultModel, TQuery> : IRepository<TEntity, TResultModel, TQuery, string>
        where TEntity : MongoDbEntity, new()
        where TResultModel : class, new()
        where TQuery : IPaginationFilter
    {
        protected readonly IMongoCollection<TEntity> Collection;
        protected readonly IMongoDatabase Database;

        protected MongoDbRepositoryBase(IMongoDatabase mongoDatabase, string collectionName)
        {
            Database = mongoDatabase;
            Collection = Database.GetCollection<TEntity>(collectionName);
        }

        public virtual IQueryable<TEntity> AsQueryable() => Collection.AsQueryable();

        public virtual async Task<IPaginationResult<ICollection<TResultModel>>> GetAsync(TQuery query, Expression<Func<TEntity, object>> sortField = null, bool desc = false)
        {
            var builder = Builders<TEntity>.Filter;
            var filter = builder.Empty;

            var queryProps = typeof(TQuery).GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .Where(x => !x.GetCustomAttributes(typeof(BsonIgnoreAttribute), false).Any());

            foreach (var propertyInfo in queryProps)
            {
                var value = propertyInfo.GetValue(query);
                if (value != default)
                {
                    filter &= builder.Eq(propertyInfo.Name, value);
                }
            }

            var totalCount = await Collection
                .Find(filter).CountDocumentsAsync();
            
            List<TEntity>? entities = null;

            if (totalCount != 0)
            {
                var findFluent = Collection.Find(filter)
                    .Skip(query.Skip)
                    .Limit(query.PageSize);

                sortField ??= entity => entity.Id;

                findFluent = desc ? findFluent.SortByDescending(sortField) : findFluent.SortBy(sortField);

                entities = await findFluent.ToListAsync();
            }

            var result = entities?.Adapt<ICollection<TResultModel>>();
            return new PaginationResult<ICollection<TResultModel>>(result, query, totalCount);
        }

        public virtual async Task<TResultModel> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = await Collection.Find(predicate).FirstOrDefaultAsync();
            return entity?.Adapt<TResultModel>();
        }

        public virtual async Task<TResultModel> GetByIdAsync(string id)
        {
            var entity = await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return entity?.Adapt<TResultModel>();
        }

        public virtual async Task<TResultModel> CreateAsync(TEntity entity)
        {
            if (entity == default)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var options = new InsertOneOptions { BypassDocumentValidation = false };
            await Collection.InsertOneAsync(entity, options);
            return entity.Adapt<TResultModel>();
        }

        public virtual async Task<bool> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            var writeModels = entities.Select(entity => new InsertOneModel<TEntity>(entity)).Cast<WriteModel<TEntity>>().ToList();
            var options = new BulkWriteOptions { IsOrdered = false, BypassDocumentValidation = false };
            return (await Collection.BulkWriteAsync(writeModels, options))
                .IsAcknowledged;
        }

        public async Task<bool> AddOrUpdateRangeAsync(IEnumerable<TEntity> entities)
        {
            var builder = Builders<TEntity>.Filter;
            FilterDefinition<TEntity> filter;
            var writeModels = entities.Select(entity =>
                {
                    filter = builder.Eq(nameof(entity.Id), entity.Id);
                    return new ReplaceOneModel<TEntity>(filter, entity) { IsUpsert = true };
                })
                .Cast<WriteModel<TEntity>>().ToList();
            var options = new BulkWriteOptions { IsOrdered = false, BypassDocumentValidation = false };
            return (await Collection.BulkWriteAsync(writeModels, options))
                .IsAcknowledged;
        }

        public async Task<bool> AddOrUpdateRangeAsync<TField>(IEnumerable<TEntity> entities, Expression<Func<TEntity, TField>> filterField)
        {
            var filterFieldValue = filterField.Compile();
            var writeModels = entities.Select(entity =>
                {
                    var filter = Builders<TEntity>.Filter.Eq(filterField, filterFieldValue(entity));
                    return new ReplaceOneModel<TEntity>(filter, entity) { IsUpsert = true };
                })
                .Cast<WriteModel<TEntity>>().ToList();
            var options = new BulkWriteOptions { IsOrdered = false, BypassDocumentValidation = false };
            return (await Collection.BulkWriteAsync(writeModels, options))
                .IsAcknowledged;
        }

        public async Task UpdateAsync(TEntity entity)
        {
            entity.ModifiedAt = DateTime.UtcNow;
            var updateDefinitionBuilder = new UpdateDefinitionBuilder<TEntity>();
            UpdateDefinition<TEntity> updateDefinition = null;

            foreach (var property in typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).Where(x => !x.GetCustomAttributes(typeof(BsonIgnoreAttribute), false).Any()))
            {
                updateDefinition = updateDefinition == null
                    ? updateDefinitionBuilder.Set(property.Name, property.GetValue(entity))
                    : updateDefinition.Set(property.Name, property.GetValue(entity));
            }

            if (updateDefinition == null)
            {
                return;
            }

            await Collection.FindOneAndUpdateAsync(x => x.Id == entity.Id && !x.IsDeleted, updateDefinition);
        }

        public virtual async Task DeleteAsync(string id)
        {
            var updateDefinitionBuilder = new UpdateDefinitionBuilder<TEntity>();
            var updateDefinition = updateDefinitionBuilder.Set(x => x.IsDeleted, true);
            updateDefinition.Set(x => x.ModifiedAt, DateTime.UtcNow);
            await Collection.FindOneAndUpdateAsync(x => x.Id == id && !x.IsDeleted, updateDefinition);
        }

        public virtual async Task DeleteAsync(TEntity entity)
        {
            await DeleteAsync(entity.Id);
        }
    }
}