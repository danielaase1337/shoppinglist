using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
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

#if !DEBUG
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IGoogleDbContext, GoogleDbContext>();
                    s.AddSingleton<IGenericRepository<ShoppingList>, GoogleFireBaseGenericRepository<ShoppingList>>();
                    s.AddSingleton<IGenericRepository<ShopItem>, GoogleFireBaseGenericRepository<ShopItem>>();
                    s.AddAutoMapper(Assembly.GetExecutingAssembly());

                })
                .Build();
#endif
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IGoogleDbContext, GoogleDbContext>();
                    s.AddSingleton<IGenericRepository<ShoppingList>, MemoryGenericRepository<ShoppingList>>();
                    s.AddSingleton<IGenericRepository<ShopItem>, MemoryGenericRepository<ShopItem>>();
                    s.AddAutoMapper(Assembly.GetExecutingAssembly());

                })
                .Build();
            host.Run();
        }
    }
}