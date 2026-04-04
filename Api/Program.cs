using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                    // Use memory repos if: local Development, OR GOOGLE_CLOUD_PROJECT not set, OR GOOGLE_APPLICATION_CREDENTIALS not set.
                    // The third check catches staging environments that have GOOGLE_CLOUD_PROJECT but no Firestore credentials file —
                    // without it, GoogleFireBaseGenericRepository throws at startup and the Functions host returns HTML for all requests.
                    var useMemoryDb = environment == "Development" || 
                                    string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")) ||
                                    string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"));

                    if (useMemoryDb)
                    {
                        // Development or local - use memory repositories
                        services.AddSingleton<IGenericRepository<ShoppingList>, MemoryGenericRepository<ShoppingList>>();
                        services.AddSingleton<IGenericRepository<ShopItem>, MemoryGenericRepository<ShopItem>>();
                        services.AddSingleton<IGenericRepository<ItemCategory>, MemoryGenericRepository<ItemCategory>>();
                        services.AddSingleton<IGenericRepository<Shop>, MemoryGenericRepository<Shop>>();
                        services.AddSingleton<IGenericRepository<FrequentShoppingList>, MemoryGenericRepository<FrequentShoppingList>>();
                        services.AddSingleton<IGenericRepository<MealRecipe>, MemoryGenericRepository<MealRecipe>>();
                        services.AddSingleton<IGenericRepository<WeekMenu>, MemoryGenericRepository<WeekMenu>>();
                        services.AddSingleton<IGenericRepository<FamilyProfile>, MemoryGenericRepository<FamilyProfile>>();
                        services.AddSingleton<IGenericRepository<PortionRule>, MemoryGenericRepository<PortionRule>>();
                        services.AddSingleton<IGenericRepository<InventoryItem>, MemoryGenericRepository<InventoryItem>>();
                        // MealIngredient: embedded in MealRecipe — no separate repository (D3/D9)
                        // DailyMeal: embedded in WeekMenu — no separate repository (D3)
                    }
                    else
                    {
                        // Production - use Firestore repositories
                        services.AddSingleton<IGenericRepository<ShoppingList>, GoogleFireBaseGenericRepository<ShoppingList>>();
                        services.AddSingleton<IGenericRepository<ShopItem>, GoogleFireBaseGenericRepository<ShopItem>>();
                        services.AddSingleton<IGenericRepository<ItemCategory>, GoogleFireBaseGenericRepository<ItemCategory>>();
                        services.AddSingleton<IGenericRepository<Shop>, GoogleFireBaseGenericRepository<Shop>>();
                        services.AddSingleton<IGenericRepository<FrequentShoppingList>, GoogleFireBaseGenericRepository<FrequentShoppingList>>();
                        services.AddSingleton<IGenericRepository<MealRecipe>, GoogleFireBaseGenericRepository<MealRecipe>>();
                        services.AddSingleton<IGenericRepository<WeekMenu>, GoogleFireBaseGenericRepository<WeekMenu>>();
                        services.AddSingleton<IGenericRepository<FamilyProfile>, GoogleFireBaseGenericRepository<FamilyProfile>>();
                        services.AddSingleton<IGenericRepository<PortionRule>, GoogleFireBaseGenericRepository<PortionRule>>();
                        services.AddSingleton<IGenericRepository<InventoryItem>, GoogleFireBaseGenericRepository<InventoryItem>>();
                        // MealIngredient: embedded in MealRecipe — no separate repository (D3/D9)
                        // DailyMeal: embedded in WeekMenu — no separate repository (D3)
                    }
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Auth: x-ms-client-principal parsing enabled (SWA Microsoft provider)");

            host.Run();
        }
    }
}
