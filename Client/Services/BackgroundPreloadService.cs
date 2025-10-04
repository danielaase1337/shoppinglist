using BlazorApp.Client.Services;

namespace BlazorApp.Client.Services
{
    public interface IBackgroundPreloadService
    {
        Task StartPreloadingAsync();
        Task StartFastCorePreloadAsync();
    }

    public class BackgroundPreloadService : IBackgroundPreloadService
    {
        private readonly IDataCacheService _dataCache;
        private bool _preloadingStarted = false;

        public BackgroundPreloadService(IDataCacheService dataCache)
        {
            _dataCache = dataCache;
        }

        public async Task StartPreloadingAsync()
        {
            if (_preloadingStarted)
            {
                Console.WriteLine("🔄 Background preloading already started, skipping...");
                return;
            }

            _preloadingStarted = true;
            Console.WriteLine("🚀 Starting background preload service...");

            // Start preloading in background without blocking UI
            _ = Task.Run(async () =>
            {
                try
                {
                    // Small delay to let the initial page load complete
                    await Task.Delay(2000);
                    
                    Console.WriteLine("🔄 Beginning background preload of active shopping lists...");
                    
                    // Preload active shopping lists
                    await _dataCache.PreloadActiveShoppingListsAsync();
                    
                    Console.WriteLine("✅ Background preloading completed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Background preloading failed: {ex.Message}");
                }
            });
        }

        public async Task StartFastCorePreloadAsync()
        {
            Console.WriteLine("⚡ Starting fast core data preload...");
            
            try
            {
                // Start fast parallel loading immediately - no delay
                await _dataCache.PreloadCoreDataAsync();
                Console.WriteLine("✅ Fast core data preload completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fast core data preload failed: {ex.Message}");
            }
        }
    }
}