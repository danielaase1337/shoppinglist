using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Repository;
using System.Reflection;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;

namespace ApiIsolated
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<IGoogleDbContext, GoogleDbContext>();
                    services.AddAutoMapper(Assembly.GetExecutingAssembly());

                    // Use environment variable to determine which repository to use
                    var environment = context.HostingEnvironment.EnvironmentName;
                    var useMemoryDb = environment == "Development" || 
                                    string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT"));

                    if (useMemoryDb)
                    {
                        // Development or local - use memory repositories
                        services.AddSingleton<IGenericRepository<ShoppingList>, MemoryGenericRepository<ShoppingList>>();
                        services.AddSingleton<IGenericRepository<ShopItem>, MemoryGenericRepository<ShopItem>>();
                        services.AddSingleton<IGenericRepository<ItemCategory>, MemoryGenericRepository<ItemCategory>>();
                        services.AddSingleton<IGenericRepository<Shop>, MemoryGenericRepository<Shop>>();
                        services.AddSingleton<IGenericRepository<FrequentShoppingList>, MemoryGenericRepository<FrequentShoppingList>>();
                    }
                    else
                    {
                        // Production - use Firestore repositories
                        services.AddSingleton<IGenericRepository<ShoppingList>, GoogleFireBaseGenericRepository<ShoppingList>>();
                        services.AddSingleton<IGenericRepository<ShopItem>, GoogleFireBaseGenericRepository<ShopItem>>();
                        services.AddSingleton<IGenericRepository<ItemCategory>, GoogleFireBaseGenericRepository<ItemCategory>>();
                        services.AddSingleton<IGenericRepository<Shop>, GoogleFireBaseGenericRepository<Shop>>();
                        services.AddSingleton<IGenericRepository<FrequentShoppingList>, GoogleFireBaseGenericRepository<FrequentShoppingList>>();
                    }
                })
                .Build();
            
            host.Run();
        }
    }
}