using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Api.Controllers
{
    public class ShopsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<Shop> repository;
        private readonly IGenericRepository<ShoppingList> shoppingListRepository;
        private readonly IMapper mapper;

        public ShopsController(ILoggerFactory loggerFactory, IGenericRepository<Shop> repository, IGenericRepository<ShoppingList> shoppingListRepository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ShopsController>();
            this.repository = repository;
            this.shoppingListRepository = shoppingListRepository;
            this.mapper = mapper;
        }

        [Function("shops")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
        {
            try
            {
                if (req.Method == "GET")
                {
                    var resultingShops = await repository.Get();
                    if (resultingShops == null)
                    {
                        return await GetErroRespons("Could not get shops", req);
                    }

                    var okResult = req.CreateResponse(HttpStatusCode.OK);
                    if (!resultingShops.Any()) await okResult.WriteAsJsonAsync(new List<ShopModel>());
                    else
                        await okResult.WriteAsJsonAsync(mapper.Map<ShopModel[]>(resultingShops));

                    return okResult;
                }
                else
                {
                    var newShop = await req.ReadFromJsonAsync<ShopModel>();
                    if (newShop == null) return await GetErroRespons("No content in shop body", req);
                    Shop updatOrInsert = mapper.Map<Shop>(newShop);
                    if (req.Method == "POST")
                    {
                        updatOrInsert = await repository.Insert(updatOrInsert);
                    }
                    else if (req.Method == "PUT")
                    {
                        updatOrInsert = await repository.Update(updatOrInsert);
                    }
                    if (updatOrInsert == null)
                    {
                        return await GetErroRespons($"Error in updating or inserting shot into database, base method is {req.Method}", req);

                    }
                    var okRespons = req.CreateResponse(HttpStatusCode.OK);
                    await okRespons.WriteAsJsonAsync(mapper.Map<ShopModel>(updatOrInsert));
                    return okRespons;
                }

            }
            catch (System.Exception e)
            {
                _logger.LogError(e, $"Something went wrong in shops controller, in medtod {req.Method}");
                return await GetErroRespons(e.Message, req);
                throw;
            }
        }

        [Function("shop")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "shop/{id}")] HttpRequestData req, string id)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var res = await repository.Get(id);
                    if (res == null)
                    {
                        return await GetErroRespons("could not load shop from db", req);

                    }
                    await response.WriteAsJsonAsync(mapper.Map<ShopModel>(res));
                    return response;
                }
                else if (req.Method == "DELETE")
                {
                    var shop = await repository.Get(id);
                    var shopName = shop?.Name ?? id;
                    _logger.LogInformation("Delete attempt for shop '{ShopName}' (id: {ShopId})", shopName, id);
                    var res = await repository.Delete(id);
                    if (!res)
                    {
                        return await GetErroRespons("could not delete shop from db", req);

                    }
                    return req.CreateResponse(HttpStatusCode.NoContent);
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (System.Exception e)
            {
                var msg = $"something went wrong in the shop call to id {id} - method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
        // NOTE: ShoppingList has no ShopId property — shop-based sorting is entirely client-side.
        // Therefore this endpoint will always return dependencyCount: 0.
        // If a shop reference is added to ShoppingList in the future, update the query below.
        [Function("shopdependencies")]
        public async Task<HttpResponseData> RunDependencies([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/{id}/dependencies")] HttpRequestData req, string id)
        {
            try
            {
                var shop = await repository.Get(id);
                if (shop == null)
                {
                    return await GetErroRespons($"Shop with id {id} not found", req);
                }

                var allLists = await shoppingListRepository.Get();

                // ShoppingList stores no ShopId — filter always yields empty.
                // Kept as a typed query so future shop references can plug in here.
                var dependentListNames = (allLists ?? Enumerable.Empty<ShoppingList>())
                    .Where(list => list.ListId == id)
                    .Select(list => list.Name)
                    .ToList();

                var result = new
                {
                    dependencyCount = dependentListNames.Count,
                    dependentLists = dependentListNames
                };

                _logger.LogInformation(
                    "Dependencies checked for shop '{ShopName}' (id: {ShopId}): {Count} dependent lists",
                    shop.Name, id, dependentListNames.Count);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (System.Exception e)
            {
                var msg = $"Something went wrong checking dependencies for shop id {id}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }


    }
}
