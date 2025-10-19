using BlazorApp.Client.Common;
using Shared.HandlelisteModels;
using System.Net.Http.Json;

namespace BlazorApp.Client.Services
{
    public interface IDataCacheService
    {
        Task<ICollection<ShopItemModel>> GetItemsAsync(bool forceRefresh = false);
        Task<ICollection<ItemCategoryModel>> GetCategoriesAsync(bool forceRefresh = false);
        Task<ICollection<ShopModel>> GetShopsAsync(bool forceRefresh = false);
        Task<ICollection<ShoppingListModel>> GetShoppingListsAsync(bool forceRefresh = false);
        Task<ShoppingListModel?> GetShoppingListAsync(string id, bool forceRefresh = false);
        Task<ShopModel?> GetShopAsync(string id, bool forceRefresh = false);
        
        // Frequent Shopping Lists
        Task<ICollection<FrequentShoppingListModel>> GetFrequentListsAsync(bool forceRefresh = false);
        Task<FrequentShoppingListModel?> GetFrequentListAsync(string id, bool forceRefresh = false);
        
        void InvalidateItemsCache();
        void InvalidateCategoriesCache();
        void InvalidateShopsCache();
        void InvalidateShoppingListsCache();
        void InvalidateShoppingListCache(string id);
        void InvalidateShopCache(string id);
        void InvalidateFrequentListsCache();
        void InvalidateFrequentListCache(string id);
        void InvalidateAllCaches();

        // Background preloading
        Task PreloadActiveShoppingListsAsync();
        Task PreloadCoreDataAsync();
    }

    public class DataCacheService : IDataCacheService
    {
        private readonly HttpClient _httpClient;
        private readonly ISettings _settings;
        
        // Cache storage
        private ICollection<ShopItemModel>? _cachedItems;
        private ICollection<ItemCategoryModel>? _cachedCategories;
        private ICollection<ShopModel>? _cachedShops;
        private ICollection<ShoppingListModel>? _cachedShoppingLists;
        private readonly Dictionary<string, ShoppingListModel> _cachedShoppingListDetails = new();
        private readonly Dictionary<string, ShopModel> _cachedShopDetails = new();
        private ICollection<FrequentShoppingListModel>? _cachedFrequentLists;
        private readonly Dictionary<string, FrequentShoppingListModel> _cachedFrequentListDetails = new();
        
        // Cache timestamps for TTL
        private DateTime? _itemsCacheTime;
        private DateTime? _categoriesCacheTime;
        private DateTime? _shopsCacheTime;
        private DateTime? _shoppingListsCacheTime;
        private readonly Dictionary<string, DateTime> _shoppingListDetailsCacheTime = new();
        private readonly Dictionary<string, DateTime> _shopDetailsCacheTime = new();
        private DateTime? _frequentListsCacheTime;
        private readonly Dictionary<string, DateTime> _frequentListDetailsCacheTime = new();
        
        // Cache TTL (Time To Live) - 5 minutes for most data
        private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _detailsCacheTtl = TimeSpan.FromMinutes(10); // Longer TTL for details
        
        public DataCacheService(HttpClient httpClient, ISettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task<ICollection<ShopItemModel>> GetItemsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedItems != null && IsCacheValid(_itemsCacheTime))
            {
                Console.WriteLine("🎯 Using cached items");
                return _cachedItems;
            }

            try
            {
                Console.WriteLine("🌐 Fetching items from API");
                _cachedItems = await _httpClient.GetFromJsonAsync<List<ShopItemModel>>(_settings.GetApiUrl(ShoppingListKeysEnum.shopItems)) ?? new List<ShopItemModel>();
                _itemsCacheTime = DateTime.Now;
                return _cachedItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading items: {ex.Message}");
                return _cachedItems ?? new List<ShopItemModel>();
            }
        }

        public async Task<ICollection<ItemCategoryModel>> GetCategoriesAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedCategories != null && IsCacheValid(_categoriesCacheTime))
            {
                Console.WriteLine("🎯 Using cached categories");
                return _cachedCategories;
            }

            try
            {
                Console.WriteLine("🌐 Fetching categories from API");
                _cachedCategories = await _httpClient.GetFromJsonAsync<List<ItemCategoryModel>>(_settings.GetApiUrl(ShoppingListKeysEnum.itemcategorys)) ?? new List<ItemCategoryModel>();
                _categoriesCacheTime = DateTime.Now;
                return _cachedCategories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading categories: {ex.Message}");
                return _cachedCategories ?? new List<ItemCategoryModel>();
            }
        }

        public async Task<ICollection<ShopModel>> GetShopsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedShops != null && IsCacheValid(_shopsCacheTime))
            {
                Console.WriteLine("🎯 Using cached shops");
                return _cachedShops;
            }

            try
            {
                Console.WriteLine("🌐 Fetching shops from API");
                _cachedShops = await _httpClient.GetFromJsonAsync<List<ShopModel>>(_settings.GetApiUrl(ShoppingListKeysEnum.Shops)) ?? new List<ShopModel>();
                _shopsCacheTime = DateTime.Now;
                return _cachedShops;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading shops: {ex.Message}");
                return _cachedShops ?? new List<ShopModel>();
            }
        }

        public async Task<ICollection<ShoppingListModel>> GetShoppingListsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedShoppingLists != null && IsCacheValid(_shoppingListsCacheTime))
            {
                Console.WriteLine("🎯 Using cached shopping lists");
                return _cachedShoppingLists;
            }

            try
            {
                Console.WriteLine("🌐 Fetching shopping lists from API");
                _cachedShoppingLists = await _httpClient.GetFromJsonAsync<List<ShoppingListModel>>(_settings.GetApiUrl(ShoppingListKeysEnum.ShoppingLists)) ?? new List<ShoppingListModel>();
                _shoppingListsCacheTime = DateTime.Now;
                return _cachedShoppingLists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading shopping lists: {ex.Message}");
                return _cachedShoppingLists ?? new List<ShoppingListModel>();
            }
        }

        public async Task<ShoppingListModel?> GetShoppingListAsync(string id, bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedShoppingListDetails.TryGetValue(id, out var cachedList) && 
                IsCacheValid(_shoppingListDetailsCacheTime.GetValueOrDefault(id), _detailsCacheTtl))
            {
                Console.WriteLine($"🎯 Using cached shopping list details for {id}");
                return cachedList;
            }

            try
            {
                Console.WriteLine($"🌐 Fetching shopping list {id} from API");
                var list = await _httpClient.GetFromJsonAsync<ShoppingListModel>(_settings.GetApiUrlId(ShoppingListKeysEnum.ShoppingList, id));
                if (list != null)
                {
                    _cachedShoppingListDetails[id] = list;
                    _shoppingListDetailsCacheTime[id] = DateTime.Now;
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading shopping list {id}: {ex.Message}");
                return _cachedShoppingListDetails.GetValueOrDefault(id);
            }
        }

        public async Task<ShopModel?> GetShopAsync(string id, bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedShopDetails.TryGetValue(id, out var cachedShop) && 
                IsCacheValid(_shopDetailsCacheTime.GetValueOrDefault(id), _detailsCacheTtl))
            {
                Console.WriteLine($"🎯 Using cached shop details for {id}");
                return cachedShop;
            }

            try
            {
                Console.WriteLine($"🌐 Fetching shop {id} from API");
                var shop = await _httpClient.GetFromJsonAsync<ShopModel>(_settings.GetApiUrlId(ShoppingListKeysEnum.Shop, id));
                if (shop != null)
                {
                    _cachedShopDetails[id] = shop;
                    _shopDetailsCacheTime[id] = DateTime.Now;
                }
                return shop;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading shop {id}: {ex.Message}");
                return _cachedShopDetails.GetValueOrDefault(id);
            }
        }

        // Cache invalidation methods
        public void InvalidateItemsCache()
        {
            Console.WriteLine("🗑️ Invalidating items cache");
            _cachedItems = null;
            _itemsCacheTime = null;
        }

        public void InvalidateCategoriesCache()
        {
            Console.WriteLine("🗑️ Invalidating categories cache");
            _cachedCategories = null;
            _categoriesCacheTime = null;
        }

        public void InvalidateShopsCache()
        {
            Console.WriteLine("🗑️ Invalidating shops cache");
            _cachedShops = null;
            _shopsCacheTime = null;
        }

        public void InvalidateShoppingListsCache()
        {
            Console.WriteLine("🗑️ Invalidating shopping lists cache");
            _cachedShoppingLists = null;
            _shoppingListsCacheTime = null;
        }

        public void InvalidateShoppingListCache(string id)
        {
            Console.WriteLine($"🗑️ Invalidating shopping list cache for {id}");
            _cachedShoppingListDetails.Remove(id);
            _shoppingListDetailsCacheTime.Remove(id);
        }

        public void InvalidateShopCache(string id)
        {
            Console.WriteLine($"🗑️ Invalidating shop cache for {id}");
            _cachedShopDetails.Remove(id);
            _shopDetailsCacheTime.Remove(id);
        }

        public void InvalidateAllCaches()
        {
            Console.WriteLine("🗑️ Invalidating all caches");
            _cachedItems = null;
            _cachedCategories = null;
            _cachedShops = null;
            _cachedShoppingLists = null;
            _cachedShoppingListDetails.Clear();
            _cachedShopDetails.Clear();
            _cachedFrequentLists = null;
            _cachedFrequentListDetails.Clear();
            
            _itemsCacheTime = null;
            _categoriesCacheTime = null;
            _shopsCacheTime = null;
            _shoppingListsCacheTime = null;
            _shoppingListDetailsCacheTime.Clear();
            _shopDetailsCacheTime.Clear();
            _frequentListsCacheTime = null;
            _frequentListDetailsCacheTime.Clear();
        }

        // Background preloading for active shopping lists
        public async Task PreloadActiveShoppingListsAsync()
        {
            Console.WriteLine("🔄 Starting background preload of active shopping lists...");
            
            try
            {
                // Get all shopping lists first
                var allLists = await GetShoppingListsAsync();
                if (allLists == null) return;

                // Find incomplete lists (not done)
                var activeLists = allLists.Where(list => !list.IsDone).ToList();
                Console.WriteLine($"📋 Found {activeLists.Count} active shopping lists to preload");

                if (!activeLists.Any()) return;

                // Preload each active list in background
                var preloadTasks = activeLists.Select(async list =>
                {
                    try
                    {
                        await GetShoppingListAsync(list.Id);
                        Console.WriteLine($"✅ Preloaded shopping list: {list.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to preload list {list.Name}: {ex.Message}");
                    }
                });

                // Run all preload tasks in parallel
                await Task.WhenAll(preloadTasks);
                Console.WriteLine($"🎉 Completed preloading {activeLists.Count} active shopping lists");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during background preload: {ex.Message}");
            }
        }

        // Fast initial load - preloads core data in parallel
        public async Task PreloadCoreDataAsync()
        {
            Console.WriteLine("🚀 Starting fast parallel preload of core data...");
            
            try
            {
                // Load all essential data in parallel for instant app responsiveness
                var loadItems = GetItemsAsync();
                var loadCategories = GetCategoriesAsync(); 
                var loadShops = GetShopsAsync();
                var loadShoppingLists = GetShoppingListsAsync();

                Console.WriteLine("⚡ Loading items, categories, shops, and shopping lists in parallel...");
                
                // Wait for all core data to load simultaneously
                await Task.WhenAll(loadItems, loadCategories, loadShops, loadShoppingLists);
                
                var items = await loadItems;
                var categories = await loadCategories;
                var shops = await loadShops;
                var lists = await loadShoppingLists;
                
                Console.WriteLine($"✅ Fast preload complete: {items?.Count ?? 0} items, {categories?.Count ?? 0} categories, {shops?.Count ?? 0} shops, {lists?.Count ?? 0} lists");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during fast core data preload: {ex.Message}");
            }
        }

        // Frequent Shopping Lists methods
        public async Task<ICollection<FrequentShoppingListModel>> GetFrequentListsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedFrequentLists != null && IsCacheValid(_frequentListsCacheTime))
            {
                Console.WriteLine("🎯 Using cached frequent lists");
                return _cachedFrequentLists;
            }

            try
            {
                Console.WriteLine("🌐 Fetching frequent lists from API");
                _cachedFrequentLists = await _httpClient.GetFromJsonAsync<List<FrequentShoppingListModel>>(_settings.GetApiUrl(ShoppingListKeysEnum.FrequentShoppingLists)) ?? new List<FrequentShoppingListModel>();
                _frequentListsCacheTime = DateTime.Now;
                return _cachedFrequentLists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading frequent lists: {ex.Message}");
                return _cachedFrequentLists ?? new List<FrequentShoppingListModel>();
            }
        }

        public async Task<FrequentShoppingListModel?> GetFrequentListAsync(string id, bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedFrequentListDetails.TryGetValue(id, out var cached) && IsCacheValid(_frequentListDetailsCacheTime.GetValueOrDefault(id), _detailsCacheTtl))
            {
                Console.WriteLine($"🎯 Using cached frequent list detail for {id}");
                return cached;
            }

            try
            {
                Console.WriteLine($"🌐 Fetching frequent list {id} from API");
                var list = await _httpClient.GetFromJsonAsync<FrequentShoppingListModel>(_settings.GetApiUrlId(ShoppingListKeysEnum.FrequentShoppingList, id));
                
                if (list != null)
                {
                    _cachedFrequentListDetails[id] = list;
                    _frequentListDetailsCacheTime[id] = DateTime.Now;
                }
                
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading frequent list {id}: {ex.Message}");
                return _cachedFrequentListDetails.GetValueOrDefault(id);
            }
        }

        public void InvalidateFrequentListsCache()
        {
            Console.WriteLine("🗑️ Invalidating frequent lists cache");
            _cachedFrequentLists = null;
            _frequentListsCacheTime = null;
        }

        public void InvalidateFrequentListCache(string id)
        {
            Console.WriteLine($"🗑️ Invalidating frequent list cache for {id}");
            _cachedFrequentListDetails.Remove(id);
            _frequentListDetailsCacheTime.Remove(id);
        }

        private bool IsCacheValid(DateTime? cacheTime, TimeSpan? customTtl = null)
        {
            if (!cacheTime.HasValue) return false;
            var ttl = customTtl ?? _cacheTtl;
            return DateTime.Now - cacheTime.Value < ttl;
        }
    }
}