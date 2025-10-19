
using Shared.BaseModels;
using Shared.HandlelisteModels;
using Shared.FireStoreDataModels;
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
            _ = Task.Run(async () => await AddDummyValues(typeof(TEntity)));
        }

        private async Task AddDummyValues(Type type)
        {
            // Initialize ShoppingList test data with FireStore models
            if (type == typeof(ShoppingList))
            {
                var testList1 = new ShoppingList
                {
                    Id = "test-list-1",
                    Name = "Ukeshandel",
                    IsDone = false,
                    ShoppingItems = new List<ShoppingListItem>
                    {
                        new ShoppingListItem
                        {
                            Varen = new ShopItem 
                            { 
                                Id = "milk-1", 
                                Name = "Melk", 
                                Unit = "Liter",
                                ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                            },
                            Mengde = 2,
                            IsDone = false
                        },
                        new ShoppingListItem
                        {
                            Varen = new ShopItem 
                            { 
                                Id = "bread-1", 
                                Name = "Brød", 
                                Unit = "Stk",
                                ItemCategory = new ItemCategory { Id = "bakery", Name = "Bakeri" }
                            },
                            Mengde = 1,
                            IsDone = false
                        },
                        new ShoppingListItem
                        {
                            Varen = new ShopItem 
                            { 
                                Id = "apple-1", 
                                Name = "Epler", 
                                Unit = "Kg",
                                ItemCategory = new ItemCategory { Id = "fruit", Name = "Frukt" }
                            },
                            Mengde = 1,
                            IsDone = true
                        }
                    }
                };
                if (testList1 is TEntity entity1) await Insert(entity1);

                var testList2 = new ShoppingList
                {
                    Id = "test-list-2",
                    Name = "Middag i kveld",
                    IsDone = false,
                    ShoppingItems = new List<ShoppingListItem>
                    {
                        new ShoppingListItem
                        {
                            Varen = new ShopItem 
                            { 
                                Id = "chicken-1", 
                                Name = "Kyllingfilet", 
                                Unit = "Kg",
                                ItemCategory = new ItemCategory { Id = "meat", Name = "Kjøtt" }
                            },
                            Mengde = 1,
                            IsDone = false
                        }
                    }
                };
                if (testList2 is TEntity entity2) await Insert(entity2);
            }

            // Initialize Shop test data with FireStore models
            if (type == typeof(Shop))
            {
                var remaShop = new Shop
                {
                    Id = "rema-1000",
                    Name = "Rema 1000",
                    ShelfsInShop = new List<Shelf>
                    {
                        new Shelf
                        {
                            Id = "shelf-1",
                            Name = "Inngang - Frukt og grønt",
                            SortIndex = 1,
                            ItemCateogries = new List<ItemCategory>
                            {
                                new ItemCategory { Id = "fruit", Name = "Frukt" },
                                new ItemCategory { Id = "vegetables", Name = "Grønnsaker" }
                            }
                        },
                        new Shelf
                        {
                            Id = "shelf-2",
                            Name = "Bakeri",
                            SortIndex = 2,
                            ItemCateogries = new List<ItemCategory>
                            {
                                new ItemCategory { Id = "bakery", Name = "Bakeri" }
                            }
                        },
                        new Shelf
                        {
                            Id = "shelf-3",
                            Name = "Kjøtt og fisk",
                            SortIndex = 3,
                            ItemCateogries = new List<ItemCategory>
                            {
                                new ItemCategory { Id = "meat", Name = "Kjøtt" },
                                new ItemCategory { Id = "fish", Name = "Fisk" }
                            }
                        },
                        new Shelf
                        {
                            Id = "shelf-4",
                            Name = "Meieri",
                            SortIndex = 4,
                            ItemCateogries = new List<ItemCategory>
                            {
                                new ItemCategory { Id = "dairy", Name = "Meieri" }
                            }
                        }
                    }
                };
                if (remaShop is TEntity remaEntity) await Insert(remaEntity);

                var icaShop = new Shop
                {
                    Id = "ica-maxi",
                    Name = "ICA Maxi",
                    ShelfsInShop = new List<Shelf>
                    {
                        new Shelf
                        {
                            Id = "ica-shelf-1",
                            Name = "Meieri først",
                            SortIndex = 1,
                            ItemCateogries = new List<ItemCategory>
                            {
                                new ItemCategory { Id = "dairy", Name = "Meieri" }
                            }
                        },
                        new Shelf
                        {
                            Id = "ica-shelf-2",
                            Name = "Kjøtt",
                            SortIndex = 2,
                            ItemCateogries = new List<ItemCategory>
                            {
                                new ItemCategory { Id = "meat", Name = "Kjøtt" }
                            }
                        },
                        new Shelf
                        {
                            Id = "ica-shelf-3",
                            Name = "Frukt til slutt",
                            SortIndex = 3,
                            ItemCateogries = new List<ItemCategory>
                            {
                                new ItemCategory { Id = "fruit", Name = "Frukt" }
                            }
                        }
                    }
                };
                if (icaShop is TEntity icaEntity) await Insert(icaEntity);

                // Keep existing Kiwi shop
                var kiwiShop = new Shop() { Id = "2", Name = "Kiwi lyngås" };
                if (kiwiShop is TEntity kiwiEntity) await Insert(kiwiEntity);
            }

            // Initialize ShopItem test data with FireStore models
            if (type == typeof(ShopItem))
            {
                var shopItems = new List<ShopItem>
                {
                    new ShopItem
                    {
                        Id = "milk-1",
                        Name = "Melk",
                        Unit = "Liter",
                        ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                    },
                    new ShopItem
                    {
                        Id = "bread-1",
                        Name = "Brød",
                        Unit = "Stk",
                        ItemCategory = new ItemCategory { Id = "bakery", Name = "Bakeri" }
                    },
                    new ShopItem
                    {
                        Id = "apple-1",
                        Name = "Epler",
                        Unit = "Kg",
                        ItemCategory = new ItemCategory { Id = "fruit", Name = "Frukt" }
                    },
                    new ShopItem
                    {
                        Id = "chicken-1",
                        Name = "Kyllingfilet",
                        Unit = "Kg",
                        ItemCategory = new ItemCategory { Id = "meat", Name = "Kjøtt" }
                    },
                    new ShopItem
                    {
                        Id = "banana-1",
                        Name = "Bananer",
                        Unit = "Kg",
                        ItemCategory = new ItemCategory { Id = "fruit", Name = "Frukt" }
                    },
                    new ShopItem
                    {
                        Id = "yogurt-1",
                        Name = "Yoghurt",
                        Unit = "Stk",
                        ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                    },
                    new ShopItem
                    {
                        Id = "salmon-1",
                        Name = "Laks",
                        Unit = "Kg",
                        ItemCategory = new ItemCategory { Id = "fish", Name = "Fisk" }
                    },
                    new ShopItem
                    {
                        Id = "carrots-1",
                        Name = "Gulrøtter",
                        Unit = "Kg",
                        ItemCategory = new ItemCategory { Id = "vegetables", Name = "Grønnsaker" }
                    }
                };

                foreach (var item in shopItems)
                {
                    if (item is TEntity itemEntity) await Insert(itemEntity);
                }
            }

            // Initialize Shelf test data with FireStore models
            if (type == typeof(Shelf))
            {
                var shelf1 = new Shelf()
                {
                    Id = "shelf-meieri",
                    Name = "Meieri",
                    ItemCateogries = new List<ItemCategory>()
                    { 
                        new ItemCategory() { Id = "dairy", Name = "Meieri"}
                    },
                    SortIndex = 1
                };
                if (shelf1 is TEntity shelf1Entity) await Insert(shelf1Entity);
                
                var shelf2 = new Shelf()
                {
                    Id = "shelf-drikke",
                    Name = "Mineralvann",
                    ItemCateogries = new List<ItemCategory>()
                    { 
                        new ItemCategory() { Id = "drinks", Name = "Drikke"}
                    },
                    SortIndex = 2
                };
                if (shelf2 is TEntity shelf2Entity) await Insert(shelf2Entity);
                
                var shelf3 = new Shelf()
                {
                    Id = "shelf-frukt",
                    Name = "Frukt og grønt",
                    ItemCateogries = new List<ItemCategory>()
                    { 
                        new ItemCategory() { Id = "fruit", Name = "Frukt"},
                        new ItemCategory() { Id = "vegetables", Name = "Grønnsaker"}
                    },
                    SortIndex = 3
                };
                if (shelf3 is TEntity shelf3Entity) await Insert(shelf3Entity);
            }

            // Initialize ItemCategory test data with FireStore models
            if (type == typeof(ItemCategory))
            {
                var categories = new List<ItemCategory>
                {
                    new ItemCategory() { Id = "dairy", Name = "Meieri" },
                    new ItemCategory() { Id = "bakery", Name = "Brød" },
                    new ItemCategory() { Id = "drinks", Name = "Drikke" },
                    new ItemCategory() { Id = "baby", Name = "Barnemat" },
                    new ItemCategory() { Id = "fruit", Name = "Frukt" },
                    new ItemCategory() { Id = "vegetables", Name = "Grønnsaker" },
                    new ItemCategory() { Id = "meat", Name = "Kjøtt" },
                    new ItemCategory() { Id = "fish", Name = "Fisk" }
                };

                foreach (var category in categories)
                {
                    if (category is TEntity categoryEntity) await Insert(categoryEntity);
                }
            }

            // Initialize FrequentShoppingList test data with FireStore models
            if (type == typeof(FrequentShoppingList))
            {
                var ukehandel = new FrequentShoppingList
                {
                    Id = "frequent-ukehandel",
                    Name = "Standard Ukehandel",
                    Description = "Varer som alltid må være med på ukeshandlingen",
                    Items = new List<FrequentShoppingItem>
                    {
                        new FrequentShoppingItem
                        {
                            Id = "frequent-item-1",
                            Name = "Melk",
                            Varen = new ShopItem 
                            { 
                                Id = "milk-1", 
                                Name = "Melk", 
                                Unit = "Liter",
                                ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                            },
                            StandardMengde = 3
                        },
                        new FrequentShoppingItem
                        {
                            Id = "frequent-item-2", 
                            Name = "Brød",
                            Varen = new ShopItem 
                            { 
                                Id = "bread-1", 
                                Name = "Brød", 
                                Unit = "Stk",
                                ItemCategory = new ItemCategory { Id = "bakery", Name = "Bakeri" }
                            },
                            StandardMengde = 2
                        },
                        new FrequentShoppingItem
                        {
                            Id = "frequent-item-3",
                            Name = "Yoghurt",
                            Varen = new ShopItem 
                            { 
                                Id = "yogurt-1", 
                                Name = "Yoghurt", 
                                Unit = "Stk",
                                ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                            },
                            StandardMengde = 4
                        }
                    }
                };
                if (ukehandel is TEntity ukehandelEntity) await Insert(ukehandelEntity);

                var trippeltrumf = new FrequentShoppingList
                {
                    Id = "frequent-trippeltrumf",
                    Name = "Trippeltrumf Varer",
                    Description = "Ekstra varer for trippeltrumf uker",
                    Items = new List<FrequentShoppingItem>
                    {
                        new FrequentShoppingItem
                        {
                            Id = "frequent-item-4",
                            Name = "Laks",
                            Varen = new ShopItem 
                            { 
                                Id = "salmon-1", 
                                Name = "Laks", 
                                Unit = "Kg",
                                ItemCategory = new ItemCategory { Id = "fish", Name = "Fisk" }
                            },
                            StandardMengde = 2
                        },
                        new FrequentShoppingItem
                        {
                            Id = "frequent-item-5",
                            Name = "Toalettpapir",
                            Varen = new ShopItem 
                            { 
                                Id = "toilet-paper-1", 
                                Name = "Toalettpapir", 
                                Unit = "Pakke",
                                ItemCategory = new ItemCategory { Id = "household", Name = "Husholdning" }
                            },
                            StandardMengde = 3
                        }
                    }
                };
                if (trippeltrumf is TEntity trippeltrumfEntity) await Insert(trippeltrumfEntity);
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
                    return _data.Remove(@s);
                }

                return false; // Return false if item was not found or deleted
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
                // Ensure entity has an ID
                if (string.IsNullOrEmpty(entityToUpdate.Id))
                {
                    throw new ArgumentException("Entity must have an ID to be updated", nameof(entityToUpdate));
                }

                if (_data.TryGetValue(entityToUpdate.Id, out TEntity exiting))
                {
                    _data[entityToUpdate.Id] = entityToUpdate;
                }

                return entityToUpdate;
            });
        }

        protected async Task<string> GetNextID()
        {
            // Generate a new GUID for the entity ID
            // This avoids issues with non-numeric existing IDs like "test-list-1"
            return await Task.Run(() => Guid.NewGuid().ToString());
        }
    }
}
