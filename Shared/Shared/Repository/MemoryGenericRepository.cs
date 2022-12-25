
using Shared.BaseModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Repository
{
    public class MemoryGenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : EntityBase
    {
        readonly Dictionary<string, TEntity> _data;

        public MemoryGenericRepository()
        {
            _data = new Dictionary<string, TEntity>();
            AddDummyValues(typeof(TEntity));
        }

        private async Task AddDummyValues(Type type)
        {
            //if (type == typeof(UserSettingsModel))
            //{
            //    await Insert(new UserSettingsModel() {Id = "1", ThisUser = new User() { FirstName = "Daniel", Id = "1", LastName = "Aase" } } as TEntity);
            //}
            if (type == typeof(ShopModel))
            {
                await Insert(new ShopModel() { Id = "2", Name = "Kiwi lyngås" } as TEntity);
            }
            if (type == typeof(ShelfModel))
            {
                await Insert(new ShelfModel()
                {
                    Name = "Meieri",
                    ItemCateogries = new List<ItemCategoryModel>()
                    { new ItemCategoryModel() { Name = "Meieri"}


                    },
                    SortIndex = 1
                } as TEntity);
                await Insert(new ShelfModel()
                {
                    Name = "Mineralvann",
                    ItemCateogries = new List<ItemCategoryModel>()
                    { new ItemCategoryModel() { Name = "Drikke"}


                    },
                    SortIndex = 2
                } as TEntity);
            }
            if (type == typeof(ItemCategoryModel))
            {
                await Insert(new ItemCategoryModel() { Name = "Meieri" } as TEntity);
                await Insert(new ItemCategoryModel() { Name = "Brød" } as TEntity);
                await Insert(new ItemCategoryModel() { Name = "Drikke" } as TEntity);
                await Insert(new ItemCategoryModel() { Name = "Barnemat" } as TEntity);
            }


        }

        public async Task<bool> Delete(TEntity entityToDelete)
        {
            return await Task.Run(() =>
            {
                if (_data.TryGetValue(entityToDelete.Id, out TEntity exiting))
                {
                    _data.Remove(entityToDelete.Id);
                }

                return true;
            });
        }

        public async Task<bool> Delete(object id)
        {
            return await Task.Run(() =>
            {
                if (id is string @s && _data.TryGetValue(@s, out TEntity exiting))
                {
                    _data.Remove(@s);
                }

                return true;
            });
        }

        public async Task<ICollection<TEntity>> Get()
        {
            return await Task.Run(() =>
            {
                try
                {
                    IQueryable<TEntity> query = _data.Values.AsQueryable<TEntity>();

                    return query.ToList();

                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    return null;
                }
            });
        }

        public async Task<TEntity> Get(object id)
        {
            return await Task.Run(() =>
            {
                if (id is string @s && _data.TryGetValue(@s, out TEntity exiting))
                {
                    return exiting;
                }

                return null;
            });
        }

        public async Task<TEntity> Insert(TEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Id))
            {
                entity.Id = await GetNextID();
            }

            if (_data.TryGetValue(entity.Id, out TEntity exiting))
            {
                return exiting;
            }
            else
            {
                _data.Add(entity.Id, entity);
                return entity;
            }
        }

        public async Task<TEntity> Update(TEntity entityToUpdate)
        {
            return await Task.Run(() =>
            {
                if (_data.TryGetValue(entityToUpdate.Id, out TEntity exiting))
                {
                    _data[entityToUpdate.Id] = entityToUpdate;
                }

                return entityToUpdate;
            });
        }

        protected async Task<string> GetNextID()
        {
            return await Task.Run(() =>
            {
                int next = 1;
                if (_data.Count > 0)
                {
                    var nextS = _data.Keys.Max(k => k);
                    next = int.Parse(nextS) + 1;
                }
                return next.ToString();
            });
        }
    }
}
