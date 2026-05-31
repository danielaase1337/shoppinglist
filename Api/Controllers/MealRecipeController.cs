using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
        public async Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var result = await _repository.Get();

                    if (result == null)
                    {
                        _logger.LogError("Could not get any meal recipes");
                        return await GetErroRespons("Could not get any meal recipes", req);
                    }
                    else if (!result.Any())
                    {
                        _logger.LogInformation("No meal recipes found");
                        await response.WriteAsJsonAsync(new List<MealRecipeModel>());
                        return response;
                    }

                    var sortedResult = result.OrderByDescending(r => r.PopularityScore).ToArray();
                    var mealRecipeModels = _mapper.Map<MealRecipeModel[]>(sortedResult);
                    await response.WriteAsJsonAsync(mealRecipeModels);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await req.ReadFromJsonAsync<MealRecipeModel>();
                    if (requestBody == null) return await GetNoContentRespons("No meal recipe to create", req);

                    var mealRecipe = _mapper.Map<MealRecipe>(requestBody);
                    mealRecipe.LastModified = DateTime.UtcNow;
                    mealRecipe.IsActive = true;

                    var addRes = await _repository.Insert(mealRecipe);
                    if (addRes == null)
                    {
                        _logger.LogWarning("Could not create meal recipe");
                        return await GetErroRespons("Could not create meal recipe", req);
                    }

                    await response.WriteAsJsonAsync(_mapper.Map<MealRecipeModel>(addRes));
                    return response;
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<MealRecipeModel>();
                    if (requestBody == null) return await GetNoContentRespons("No meal recipe to update", req);

                    var mealRecipe = _mapper.Map<MealRecipe>(requestBody);
                    mealRecipe.LastModified = DateTime.UtcNow;

                    var updateRes = await _repository.Update(mealRecipe);
                    if (updateRes == null)
                        return await GetErroRespons("Could not update meal recipe", req);

                    await response.WriteAsJsonAsync(_mapper.Map<MealRecipeModel>(updateRes));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of mealrecipes, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("mealrecipe")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "mealrecipe/{id}")] HttpRequestData req, string id)
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
                    // Soft delete: mark inactive rather than remove from Firestore
                    var item = await _repository.Get(id);
                    if (item == null)
                    {
                        _logger.LogWarning($"Could not find meal recipe with id {id} for soft delete");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    item.IsActive = false;
                    item.LastModified = DateTime.UtcNow;

                    var updatedItem = await _repository.Update(item);
                    if (updatedItem == null)
                        return await GetErroRespons($"Could not soft-delete meal recipe with id {id}", req);

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(_mapper.Map<MealRecipeModel>(updatedItem));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of mealrecipe/{id}, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("mealrecipesimport")]
        public async Task<HttpResponseData> RunImport([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mealrecipes/import")] HttpRequestData req)
        {
            try
            {
                var requestBody = await req.ReadFromJsonAsync<MealRecipeModel[]>();
                if (requestBody == null || !requestBody.Any())
                    return await GetNoContentRespons("No meal recipes to import", req);

                var imported = new List<MealRecipeModel>();

                foreach (var recipeModel in requestBody)
                {
                    var mealRecipe = _mapper.Map<MealRecipe>(recipeModel);
                    mealRecipe.LastModified = DateTime.UtcNow;
                    mealRecipe.IsActive = true;

                    var inserted = await _repository.Insert(mealRecipe);
                    if (inserted != null)
                        imported.Add(_mapper.Map<MealRecipeModel>(inserted));
                    else
                        _logger.LogWarning($"Could not import meal recipe: {recipeModel.Name}");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(imported);
                return response;
            }
            catch (Exception e)
            {
                var msg = "Something went wrong during meal recipe bulk import";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
    }
}
