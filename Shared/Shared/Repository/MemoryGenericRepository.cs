
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
using FirestoreMealCategory = Shared.FireStoreDataModels.MealCategory;
using FirestoreMealType = Shared.FireStoreDataModels.MealType;
using FirestoreMealEffort = Shared.FireStoreDataModels.MealEffort;

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

            // Initialize MealRecipe seed data from family dinner history (dinners.txt)
            if (type == typeof(MealRecipe))
            {
                var recipes = new List<MealRecipe>
                {
                    // KidsLike
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Pizza", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 100, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Taco", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 90, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Pannekaker", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick, PopularityScore = 77, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Pølse og potetmos", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick, PopularityScore = 65, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Grøt", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick, PopularityScore = 62, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Hamburger", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 58, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kyllingnuggets", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick, PopularityScore = 56, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fiskeburger", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick, PopularityScore = 52, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Favaffel", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 50, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Falafel", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 44, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fiskepinner", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.Frozen, Effort = FirestoreMealEffort.Quick, PopularityScore = 41, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Pølsegnocchi", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick, PopularityScore = 42, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Pølseform med makaroni", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 40, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Nachos", Category = FirestoreMealCategory.KidsLike, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 36, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

                    // Fish
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Laks", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 85, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fiskeboller", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 80, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fiskegrateng", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 80, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fiskekaker", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 60, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Salmalaks", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 54, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Laks i pita", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 47, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Hvit fisk", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 46, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fiskesuppe", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 42, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Torsk", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 40, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fiskepakke", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 38, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kveite", Category = FirestoreMealCategory.Fish, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 35, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

                    // Meat
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Lapskaus", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 78, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kjøttkaker", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 74, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Biff", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 52, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Spareribs", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 50, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Bulgogi", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 48, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kjøttboller", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 44, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Fårikål", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 37, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Raspeballer", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 40, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Vossakorv", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 40, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Benløse fugler", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 36, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Nakkekoteletter", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 36, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Finnebiff", Category = FirestoreMealCategory.Meat, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 35, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

                    // Vegetarian
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Tomatsuppe", Category = FirestoreMealCategory.Vegetarian, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 72, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Linsegryte", Category = FirestoreMealCategory.Vegetarian, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 63, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Marokkansk bønnegryte", Category = FirestoreMealCategory.Vegetarian, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 43, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Veggisburger", Category = FirestoreMealCategory.Vegetarian, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 42, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Gulrotsuppe", Category = FirestoreMealCategory.Vegetarian, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 38, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Quinoaburger", Category = FirestoreMealCategory.Vegetarian, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 35, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

                    // Chicken
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kylling Gong Bao", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 68, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Tikka masala", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 45, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kyllingform", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 46, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kyllingsuppe", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 44, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kyllinglår", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 40, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kyllingklubber", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 40, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Hønsefrikassé", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 35, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Kalkun", Category = FirestoreMealCategory.Chicken, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 34, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

                    // Pasta
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Lasagne", Category = FirestoreMealCategory.Pasta, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 75, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Spagetti og kjøttsaus", Category = FirestoreMealCategory.Pasta, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 55, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Pastaform", Category = FirestoreMealCategory.Pasta, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 48, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "One pot pasta", Category = FirestoreMealCategory.Pasta, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 35, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

                    // Celebration
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Pinnekjøtt", Category = FirestoreMealCategory.Celebration, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 38, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Ribbe", Category = FirestoreMealCategory.Celebration, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 38, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Medisterkaker", Category = FirestoreMealCategory.Celebration, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 36, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

                    // Other
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Wok", Category = FirestoreMealCategory.Other, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 38, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Enchiladas", Category = FirestoreMealCategory.Other, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 38, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                    new MealRecipe { Id = Guid.NewGuid().ToString(), Name = "Drunken noodles", Category = FirestoreMealCategory.Other, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal, PopularityScore = 34, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
                };
                foreach (var r in recipes)
                    await Insert(r as TEntity);
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
