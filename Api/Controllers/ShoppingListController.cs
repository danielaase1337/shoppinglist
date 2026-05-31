using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Api.Controllers
{
    public class GetAllShoppingListsFunction : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<ShoppingList> repo;
        private readonly IGenericRepository<InventoryItem> _inventoryRepository;

        //private readonly IGoogleDbContext dbContext;
        private readonly IMapper mapper;

        public GetAllShoppingListsFunction(ILoggerFactory loggerFactory, IGenericRepository<ShoppingList> repo, IMapper mapper, IGenericRepository<InventoryItem> inventoryRepository)
        {
            _logger = loggerFactory.CreateLogger<GetAllShoppingListsFunction>();
            this.repo = repo;
            this.mapper = mapper;
            _inventoryRepository = inventoryRepository;
        }

        [Function("shoppinglists")]
        public async Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var result = await repo.Get();

                    if (result == null)
                    {
                        _logger.Log(LogLevel.Error, $"Could not get any stored lists Error: {req.ReadAsString()}");
                        
                        return await GetErroRespons("could not get any stored lists", req);
                    }
                    else if (!result.Any())
                    {
                        _logger.LogInformation("No list found");
                        await response.WriteAsJsonAsync(new List<ShoppingListModel>());
                        return response;
                    }
                    
                    var shoppingListModel = mapper.Map<ShoppingListModel[]>(result);
                    await response.WriteAsJsonAsync(shoppingListModel);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await req.ReadFromJsonAsync<ShoppingListModel>();
                    var shoppinglist = mapper.Map<ShoppingList>(requestBody);
                    
                    // Set LastModified timestamp
                    shoppinglist.LastModified = DateTime.UtcNow;
                    
                    var addRes = await repo.Insert(shoppinglist);
                    if (addRes == null)
                    {
                        _logger.Log(LogLevel.Warning, $"Could not get shoppinglists");
                        return await GetErroRespons("could not get shoppinglist", req);
                    }
                    else
                    {
                        await response.WriteAsJsonAsync(mapper.Map<ShoppingListModel>(addRes));
                        return response;
                    }
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<ShoppingListModel>();
                    var updatedList = mapper.Map<ShoppingList>(requestBody);
                    
                    // Update LastModified timestamp
                    updatedList.LastModified = DateTime.UtcNow;
                    
                    // Fetch existing state before update for IsDone transition detection
                    var existing = await repo.Get(updatedList.Id);
                    
                    var addRes = await repo.Update(updatedList);
                    if (addRes == null)
                        return await GetErroRespons("Could not update shoppinglist", req);

                    // When a shopping list transitions from not-done to done, add items to inventory
                    if (existing != null && !existing.IsDone && updatedList.IsDone)
                    {
                        var allInventory = (await _inventoryRepository.Get()) ?? new List<InventoryItem>();
                        var inventoryByShopItemId = allInventory
                            .Where(i => i.IsActive && !string.IsNullOrEmpty(i.ShopItemId))
                            .GroupBy(i => i.ShopItemId)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                        var inventoryChanges = new Dictionary<string, InventoryItem>(StringComparer.OrdinalIgnoreCase);
 
                        foreach (var item in updatedList.ShoppingItems ?? Enumerable.Empty<ShoppingListItem>())
                        {
                            if (item?.Varen == null) continue;
 
                            // Only track items with StockBehaviour.Track (#75)
                            if (item.Varen.StockBehaviour != StockBehaviour.Track) continue;
 
                            if (!inventoryByShopItemId.TryGetValue(item.Varen.Id, out var inventoryItem))
                            {
                                inventoryItem = new InventoryItem
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    ShopItemId = item.Varen.Id,
                                    ShopItemName = item.Varen.Name,
                                    QuantityInStock = 0,
                                    IsActive = true
                                };
                                inventoryByShopItemId[item.Varen.Id] = inventoryItem;
                            }
 
                            inventoryItem.QuantityInStock += item.Mengde;
                            inventoryItem.LastModified = DateTime.UtcNow;
                            inventoryChanges[inventoryItem.Id] = inventoryItem;
                        }

                        if (inventoryChanges.Any() && !await _inventoryRepository.BatchUpdate(inventoryChanges.Values))
                            return await GetErroRespons("Could not update inventory", req);
                    }

                    await response.WriteAsJsonAsync(mapper.Map<ShoppingListModel>(addRes));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (System.Exception e)
            {
                var msg = $"Something went wrong in execution of shoppinglists, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }

        }

        [Function("shoppinglist")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "shoppinglist/{id}")] HttpRequestData req, object id)
        {
            if(req.Method== "GET")
            {
                var result = await repo.Get(id);
                if (result == null)
                {
                    _logger.LogInformation($"Could not find the list with id {id}");
                    var res = req.CreateResponse(HttpStatusCode.InternalServerError);
                    return res;
                }
                
                var shoppingListModel = mapper.Map<ShoppingListModel>(result);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(shoppingListModel);
                return response;
            }
            else if (req.Method== "DELETE")
            {
                var deleteRes = await repo.Delete(id);
                if (deleteRes)
                    return req.CreateResponse(HttpStatusCode.NoContent);
                else
                {
                    var errorrespons = req.CreateResponse(HttpStatusCode.InternalServerError);
                    errorrespons.WriteString("Could not delete item");
                }

            }
            return req.CreateResponse(HttpStatusCode.NotFound) ;
        }
       
    }
}
