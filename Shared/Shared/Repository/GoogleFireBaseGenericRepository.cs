using Shared.BaseModels;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Shared.Repository
{
    public class GoogleFireBaseGenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : EntityBase
    {
        private readonly IGoogleDbContext dbContext;

        public GoogleFireBaseGenericRepository(IGoogleDbContext dbContext)
        {
            this.dbContext = dbContext;

            if (this.dbContext.Collection == null)
            {
                dbContext.CollectionKey = dbContext.GetCollectionKey(typeof(TEntity));
                dbContext.Collection = dbContext.DB.Collection(dbContext.CollectionKey);
            }
        }

        public async Task<bool> Delete(TEntity entityToDelete)
        {
            try
            {
                var id = entityToDelete.Id;
                await dbContext.Collection.Document(id).DeleteAsync();
                return true;
            }
            catch (Exception e)
            {
                Debug.Write(e);
                return false;
            }
        }

        public async Task<bool> Delete(object id)
        {


            if (id == null)
                return false;
            try
            {
                if (id is string sId)
                {
                    if (string.IsNullOrEmpty(sId))
                        return false;
                    await dbContext.Collection.Document(sId).DeleteAsync();
                    return true;
                }
                return false;

            }
            catch (Exception e)
            {
                Debug.Write(e);
                return false;
            }
        }

        public async Task<ICollection<TEntity>> Get()
        {
            try
            {
                var snapshot = await dbContext.Collection.GetSnapshotAsync();
                var shopItems = new Collection<TEntity>();
                var res = snapshot.Documents.Select(f => f.ConvertTo<TEntity>());

                IQueryable<TEntity> query = res.AsQueryable();
                return query.ToList();
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
            return null;
        }



        public async Task<TEntity> Get(object id)
        {
            try
            {
                if (id is string stringId)
                {
                    var docref = dbContext.Collection.Document(stringId);
                    var res = await docref.GetSnapshotAsync();
                    return res.ConvertTo<TEntity>();
                }
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }

            return null;
        }


        public async Task<TEntity> Insert(TEntity entity)
        {
            try
            {
                var addedDocRes = dbContext.Collection.Document();
                entity.Id = addedDocRes.Id;
                //entity.TimeStamp = Timestamp.GetCurrentTimestamp();
                await addedDocRes.SetAsync(entity);
                return entity;
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }
            return null;
        }

        public async Task<TEntity> Update(TEntity entityToUpdate)
        {
            var updateRef = dbContext.Collection.Document(entityToUpdate.Id);
            await updateRef.SetAsync(entityToUpdate);
            return entityToUpdate;
        }
    }
}
