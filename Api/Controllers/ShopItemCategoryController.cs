using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System.Net;
using System.Threading.Tasks;

namespace Api.Controllers
{
    public class ShopItemCategoryController : ControllerBase
    {
        private readonly IGenericRepository<ItemCategory> repository;
        private readonly IMapper mapper;
        protected readonly ILogger _logger;

        public ShopItemCategoryController(ILoggerFactory loggerFactory, IGenericRepository<ItemCategory> repository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ShopItemCategoryController>();
            this.repository = repository;
            this.mapper = mapper;
        }

        [Function("itemcategorys")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
        {
            var okRespons = req.CreateResponse(HttpStatusCode.OK);
            if (req.Method == "GET")
            {
                var itemCategories = await repository.Get();
                if (itemCategories == null)
                {
                    return await GetErroRespons("Could not load items categorys", req);
                }
                await okRespons.WriteAsJsonAsync(itemCategories);

            }
            else
            {
                var frombody = await req.ReadFromJsonAsync<ItemCategoryModel>();
                if (frombody == null)
                {
                    return await GetErroRespons("could not read item category from body", req);

                }
                var toInsert = mapper.Map<ItemCategory>(frombody);
                if (req.Method == "POST")
                {
                    var newItemCat = await repository.Insert(toInsert);
                    if (newItemCat == null)
                    {
                        return await GetErroRespons("could not add item category", req);
                    }
                    else
                        await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategory>(newItemCat));
                }
                else if (req.Method == "PUT")
                {
                    var newItemCat = await repository.Update(toInsert);
                    if (newItemCat == null)
                    {
                        return await GetErroRespons("could not add item category", req);
                    }
                    else
                    {
                        await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategory>(newItemCat));
                    }
                }
            }
            return okRespons;
        }
        [Function("itemcategory")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "itemcategory/{id}")] HttpRequestData req, object id)
        {
            var okRespons = req.CreateResponse(HttpStatusCode.OK);

            if (req.Method == "GET")
            {
                var itemCategories = await repository.Get(id);
                if (itemCategories == null)
                {
                    return await GetErroRespons("Could not load items categorys", req);
                }
                await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategory>(itemCategories));

            }
            else if (req.Method == "DELETE")
            {
                var deleteResult = await repository.Delete(id);
                if (deleteResult)
                    return req.CreateResponse(HttpStatusCode.NoContent);
                else
                    return await GetErroRespons($"Could not delete item {id}", req);
            }
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
