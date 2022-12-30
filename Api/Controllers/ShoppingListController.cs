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
    public class GetAllShoppingListsFunction : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<ShoppingList> repo;

        //private readonly IGoogleDbContext dbContext;
        private readonly IMapper mapper;

        public GetAllShoppingListsFunction(ILoggerFactory loggerFactory, IGenericRepository<ShoppingList> repo, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<GetAllShoppingListsFunction>();
            this.repo = repo;
            this.mapper = mapper;
        }

        [Function("shoppinglists")]
        public async Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Function, "get", "post", "put")] HttpRequestData req)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var result = await repo.Get();

                    if (result == null)
                    {
                        return GetErroRespons("could not get any stored lists", req);
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
                    var addRes = await repo.Insert(shoppinglist);
                    if (addRes == null)
                        return GetErroRespons("could not get shoppinglist", req);
                    else
                    {
                        await response.WriteAsJsonAsync(mapper.Map<ShoppingListModel>(addRes));
                        return response;
                    }
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<ShoppingListModel>();
                    var shoppinglist = mapper.Map<ShoppingList>(requestBody);
                    var addRes = await repo.Update(shoppinglist);
                    if (addRes == null)
                        return GetErroRespons("Could not update shoppinglist", req);
                    else
                    {
                        await response.WriteAsJsonAsync(mapper.Map<ShoppingListModel>(addRes));
                        return response;
                    }
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (System.Exception e)
            {
                var msg = $"Something went wrong in execution of shoppinglists, method type {req.Method}";
                _logger.LogError(e, msg);
                return GetErroRespons(msg, req);
            }

        }

        [Function("shoppinglist")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Function, "get", "delete", Route = "shoppinglist/{id}")] HttpRequestData req, object id)
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
            else if (req.Method== "DELTE")
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
