using System.Net;
using Shared.HandlelisteModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.FireStoreDataModels;
using Shared.Repository;
using AutoMapper;
using Grpc.Core;
using Microsoft.VisualBasic;
using System;
using System.Threading.Tasks;

namespace Api.Controllers
{
    public class ShopsItemsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<ShopItem> shopItemsRepo;
        private readonly IMapper mapper;

        public ShopsItemsController(ILoggerFactory loggerFactory, IGenericRepository<ShopItem> shopItemsRepo, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ShopsItemsController>();
            this.shopItemsRepo = shopItemsRepo;
            this.mapper = mapper;
        }

        [Function("shopitems")]
        public async  Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
        {

            try
            {
                _logger.LogInformation("C# HTTP trigger function processed a request.");
                if (req.Method == "GET")
                {
                    var items = await shopItemsRepo.Get();
                    if (items == null) return GetErroRespons("Could not get ShopsItems", req);

                    var itemsModels = mapper.Map<ShopItemModel[]>(items);
                    var getRespons = req.CreateResponse(HttpStatusCode.OK);
                    await getRespons.WriteAsJsonAsync(itemsModels);
                    return getRespons;

                }
                ShopItem dbItem = null;
                var itemToInsert = await req.ReadFromJsonAsync<ShopItemModel>();
                if (itemToInsert == null) return GetNoContentRespons($"No item to {req.Method}", req);
                else
                    dbItem = mapper.Map<ShopItem>(itemToInsert);

                ShopItem updatedShopItem = null;
                if (req.Method == "POST")
                {
                    updatedShopItem = await shopItemsRepo.Insert(dbItem);
                }
                if (req.Method == "PUT")
                {
                    updatedShopItem = await shopItemsRepo.Update(dbItem);
                }
                if (updatedShopItem == null)
                {
                    return GetErroRespons("Could update or insert shop item", req);

                }
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(mapper.Map<ShopItemModel>(updatedShopItem));
                return response;
            }
            catch (System.Exception e)
            {
                _logger.LogInformation(e.Message);
                return GetErroRespons(e.Message, req);

            }
        }



        [Function("shopitem")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "shopitem/{id}")] HttpRequestData req, string id)
        {
            try
            {
                var okRespons = req.CreateResponse(HttpStatusCode.OK);
                if (req.Method != "GET" && req.Method != "DELETE") { return req.CreateResponse(HttpStatusCode.BadRequest); }

                if (req.Method == "GET")
                {
                    var result = await shopItemsRepo.Get(id);
                    if (result == null)
                    {
                        return GetErroRespons($"Fant ikke shop item med id {id}\"", req);
                    }
                    await okRespons.WriteAsJsonAsync(mapper.Map<ShopItemModel>(result));
                    return okRespons;

                }
                else //delete
                {
                    var deleteResult = await shopItemsRepo.Delete(id);
                    if (!deleteResult)
                    {
                        return GetErroRespons($"Could not delete item with id {id}", req);
                    }
                    return req.CreateResponse(HttpStatusCode.NoContent);
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
                return GetErroRespons(e.Message, req);
            }

        }
    }
}
