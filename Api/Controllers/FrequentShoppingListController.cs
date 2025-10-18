using System.Net;
using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.HandlelisteModels;
using Shared.FireStoreDataModels;
using Shared.Repository;
using System.Text.Json;

namespace Api.Controllers
{
    public class FrequentShoppingListController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<FrequentShoppingList> _repository;
        private readonly IMapper _mapper;

        public FrequentShoppingListController(ILoggerFactory loggerFactory, IGenericRepository<FrequentShoppingList> repository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<FrequentShoppingListController>();
            _repository = repository;
            _mapper = mapper;
        }

        [Function("frequentshoppinglists")]
        public async Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var allLists = await _repository.Get();
                    var models = _mapper.Map<ICollection<FrequentShoppingListModel>>(allLists);
                    
                    response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(models);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var newListModel = JsonSerializer.Deserialize<FrequentShoppingListModel>(requestBody);
                    
                    if (newListModel == null)
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        await response.WriteStringAsync("Invalid request body");
                        return response;
                    }

                    var newList = _mapper.Map<FrequentShoppingList>(newListModel);
                    var createdList = await _repository.Insert(newList);
                    var createdModel = _mapper.Map<FrequentShoppingListModel>(createdList);
                    
                    response = req.CreateResponse(HttpStatusCode.Created);
                    await response.WriteAsJsonAsync(createdModel);
                    return response;
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var updateListModel = JsonSerializer.Deserialize<FrequentShoppingListModel>(requestBody);
                    
                    if (updateListModel == null)
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        await response.WriteStringAsync("Invalid request body");
                        return response;
                    }

                    var updateList = _mapper.Map<FrequentShoppingList>(updateListModel);
                    var updatedList = await _repository.Update(updateList);
                    var updatedModel = _mapper.Map<FrequentShoppingListModel>(updatedList);
                    
                    response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(updatedModel);
                    return response;
                }
                else
                {
                    response = req.CreateResponse(HttpStatusCode.MethodNotAllowed);
                    await response.WriteStringAsync("Method not allowed");
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FrequentShoppingListController.RunAll");
                return GetErroRespons(ex.Message, req);
            }
        }

        [Function("frequentshoppinglist")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "frequentshoppinglist/{id}")] HttpRequestData req, string id)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var list = await _repository.Get(id);
                    if (list == null)
                    {
                        response = req.CreateResponse(HttpStatusCode.NotFound);
                        await response.WriteStringAsync("Frequent shopping list not found");
                        return response;
                    }

                    var model = _mapper.Map<FrequentShoppingListModel>(list);
                    response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(model);
                    return response;
                }
                else if (req.Method == "DELETE")
                {
                    var deleteResult = await _repository.Delete(id);
                    if (!deleteResult)
                    {
                        response = req.CreateResponse(HttpStatusCode.NotFound);
                        await response.WriteStringAsync("Frequent shopping list not found");
                        return response;
                    }

                    response = req.CreateResponse(HttpStatusCode.NoContent);
                    return response;
                }
                else
                {
                    response = req.CreateResponse(HttpStatusCode.MethodNotAllowed);
                    await response.WriteStringAsync("Method not allowed");
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FrequentShoppingListController.RunOne");
                return GetErroRespons(ex.Message, req);
            }
        }
    }
}