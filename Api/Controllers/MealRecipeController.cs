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
    public class MealRecipeController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<MealRecipe> _repository;
        private readonly IMapper _mapper;

        public MealRecipeController(ILoggerFactory loggerFactory, IGenericRepository<MealRecipe> repository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<MealRecipeController>();
            _repository = repository;
            _mapper = mapper;
        }

        [Function("mealrecipes")]
        public async Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Function, "get", "post", "put")] HttpRequestData req)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var result = await _repository.Get();

                    if (result == null)
                    {
                        _logger.LogError($"Could not get any meal recipes. Error: {req.ReadAsString()}");
                        return await GetErroRespons("Could not get any meal recipes", req);
                    }
                    else if (!result.Any())
                    {
                        _logger.LogInformation("No meal recipes found");
                        await response.WriteAsJsonAsync(new List<MealRecipeModel>());
                        return response;
                    }
                    
                    // Sort by popularity score (descending)
                    var sortedResult = result.OrderByDescending(r => r.PopularityScore).ToArray();
                    var mealRecipeModels = _mapper.Map<MealRecipeModel[]>(sortedResult);
                    await response.WriteAsJsonAsync(mealRecipeModels);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await req.ReadFromJsonAsync<MealRecipeModel>();
                    var mealRecipe = _mapper.Map<MealRecipe>(requestBody);
                    
                    // Set LastModified timestamp
                    mealRecipe.LastModified = DateTime.UtcNow;
                    
                    var addRes = await _repository.Insert(mealRecipe);
                    if (addRes == null)
                    {
                        _logger.LogWarning("Could not create meal recipe");
                        return await GetErroRespons("Could not create meal recipe", req);
                    }
                    else
                    {
                        await response.WriteAsJsonAsync(_mapper.Map<MealRecipeModel>(addRes));
                        return response;
                    }
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<MealRecipeModel>();
                    var mealRecipe = _mapper.Map<MealRecipe>(requestBody);
                    
                    // Update LastModified timestamp
                    mealRecipe.LastModified = DateTime.UtcNow;
                    
                    var updateRes = await _repository.Update(mealRecipe);
                    if (updateRes == null)
                        return await GetErroRespons("Could not update meal recipe", req);
                    else
                    {
                        await response.WriteAsJsonAsync(_mapper.Map<MealRecipeModel>(updateRes));
                        return response;
                    }
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (System.Exception e)
            {
                var msg = $"Something went wrong in execution of mealrecipes, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("mealrecipe")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Function, "get", "delete", Route = "mealrecipe/{id}")] HttpRequestData req, object id)
        {
            try
            {
                if (req.Method == "GET")
                {
                    var result = await _repository.Get(id);
                    if (result == null)
                    {
                        _logger.LogInformation($"Could not find meal recipe with id {id}");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                    
                    var mealRecipeModel = _mapper.Map<MealRecipeModel>(result);
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(mealRecipeModel);
                    return response;
                }
                else if (req.Method == "DELETE")
                {
                    var delRes = await _repository.Delete(id);
                    if (delRes == null)
                    {
                        _logger.LogWarning($"Could not delete meal recipe with id {id}");
                        return await GetErroRespons($"Could not delete meal recipe with id {id}", req);
                    }
                    else
                    {
                        var response = req.CreateResponse(HttpStatusCode.OK);
                        await response.WriteAsJsonAsync(_mapper.Map<MealRecipeModel>(delRes));
                        return response;
                    }
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (System.Exception e)
            {
                var msg = $"Something went wrong in execution of mealrecipe/{id}, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
    }
}
