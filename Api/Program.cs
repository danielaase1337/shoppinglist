using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Repository;
using System.Reflection;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Microsoft.Azure.Functions.Worker;

namespace ApiIsolated
{
    public class Program
    {
        public static void Main()
        {

#if DEBUG
            //var host = new HostBuilder()
            //    .ConfigureFunctionsWorkerDefaults()
            //    .ConfigureServices(s =>
            //    {
            //        s.AddTransient<IGoogleDbContext, GoogleDbContext>();
            //        s.AddSingleton<IGenericRepository<ShoppingList>, GoogleFireBaseGenericRepository<ShoppingList>>();
            //        s.AddSingleton<IGenericRepository<ShopItem>, GoogleFireBaseGenericRepository<ShopItem>>();
            //        s.AddSingleton<IGenericRepository<ItemCategory>, GoogleFireBaseGenericRepository<ItemCategory>>();
            //        s.AddSingleton<IGenericRepository<Shop>, GoogleFireBaseGenericRepository<Shop>>();
            //        s.AddAutoMapper(Assembly.GetExecutingAssembly());

            //    })
            //    .Build();
            //host.Run();

            var host = new HostBuilder()
              .ConfigureFunctionsWebApplication()
              .ConfigureServices(s =>
              {
                  s.AddTransient<IGoogleDbContext, GoogleDbContext>();
                  s.AddSingleton<IGenericRepository<ShoppingList>, MemoryGenericRepository<ShoppingList>>();
                  s.AddSingleton<IGenericRepository<ShopItem>, MemoryGenericRepository<ShopItem>>();
                  s.AddSingleton<IGenericRepository<ItemCategory>, MemoryGenericRepository<ItemCategory>>();
                  s.AddSingleton<IGenericRepository<Shop>, MemoryGenericRepository<Shop>>();
                  s.AddAutoMapper(Assembly.GetExecutingAssembly());
                  s.AddApplicationInsightsTelemetryWorkerService();
                  s.ConfigureFunctionsApplicationInsights();

              })

              .Build();
            host.Run();
#else
          
             var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                    s.AddTransient<IGoogleDbContext, GoogleDbContext>();
                    s.AddSingleton<IGenericRepository<ShoppingList>, GoogleFireBaseGenericRepository<ShoppingList>>();
                    s.AddSingleton<IGenericRepository<ShopItem>, GoogleFireBaseGenericRepository<ShopItem>>();
                    s.AddSingleton<IGenericRepository<ItemCategory>, GoogleFireBaseGenericRepository<ItemCategory>>();
                    s.AddSingleton<IGenericRepository<Shop>, GoogleFireBaseGenericRepository<Shop>>();
                    s.AddAutoMapper(Assembly.GetExecutingAssembly());
                    s.AddApplicationInsightsTelemetryWorkerService();
                  s.ConfigureFunctionsApplicationInsights();

                })
                .Build();
            host.Run();
#endif
        }
    }
}