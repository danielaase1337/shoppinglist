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
        private readonly IMapper mapper;

        public ShopsController(ILoggerFactory loggerFactory, IGenericRepository<Shop> repository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ShopsController>();
            this.repository = repository;
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
                        return GetErroRespons("Could not get shops", req);
                    }

                    var okResult = req.CreateResponse(HttpStatusCode.OK);
                    if (!resultingShops.Any()) await okResult.WriteAsJsonAsync(new List<ShopModel>());
                    else
                        await okResult.WriteAsJsonAsync(mapper.Map<ShopModel[]>(resultingShops));

                    return okResult;
                }
                else
                {
                    var newShop = req.ReadFromJsonAsync<ShopModel>();
                    if (newShop.Result == null) return GetErroRespons("No content in shop body", req);
                    Shop updatOrInsert = mapper.Map<Shop>(newShop.Result);
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
                        return GetErroRespons($"Error in updating or inserting shot into database, base method is {req.Method}", req);

                    }
                    var okRespons = req.CreateResponse(HttpStatusCode.OK);
                    await okRespons.WriteAsJsonAsync(mapper.Map<ShopModel>(updatOrInsert));
                    return okRespons;
                }

            }
            catch (System.Exception e)
            {
                _logger.LogError(e, $"Something went wrong in shops controller, in medtod {req.Method}");
                return GetErroRespons(e.Message, req);
                throw;
            }
        }

        [Function("shop")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "shop/{id}")] HttpRequestData req, int id)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var res = await repository.Get(id);
                    if (res == null)
                    {
                        return GetErroRespons("could not load shop from db", req);

                    }
                    await response.WriteAsJsonAsync(mapper.Map<ShopModel>(res));
                    return response;
                }
                else if (req.Method == "DELETE")
                {
                    var res = await repository.Delete(id);
                    if (!res)
                    {
                        return GetErroRespons("could not delete shop from db", req);

                    }
                    return req.CreateResponse(HttpStatusCode.NoContent);
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (System.Exception e)
            {
                var msg = $"something went wrong in the shop call to id {id} - method type {req.Method}";
                _logger.LogError(e, msg);
                return GetErroRespons(msg, req);
            }
        }


    }
}
