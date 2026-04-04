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
    public class WeekMenuController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<WeekMenu> _repository;
        private readonly IGenericRepository<MealRecipe> _mealRepository;
        private readonly IMapper _mapper;

        public WeekMenuController(
            ILoggerFactory loggerFactory,
            IGenericRepository<WeekMenu> repository,
            IGenericRepository<MealRecipe> mealRepository,
            IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<WeekMenuController>();
            _repository = repository;
            _mealRepository = mealRepository;
            _mapper = mapper;
        }

        [Function("weekmenus")]
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
                        _logger.LogError("Could not get any week menus");
                        return await GetErroRespons("Could not get any week menus", req);
                    }
                    else if (!result.Any())
                    {
                        _logger.LogInformation("No week menus found");
                        await response.WriteAsJsonAsync(new List<WeekMenuModel>());
                        return response;
                    }

                    var sortedResult = result
                        .OrderByDescending(r => r.Year)
                        .ThenByDescending(r => r.WeekNumber)
                        .ToArray();
                    var weekMenuModels = _mapper.Map<WeekMenuModel[]>(sortedResult);
                    await response.WriteAsJsonAsync(weekMenuModels);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await req.ReadFromJsonAsync<WeekMenuModel>();
                    if (requestBody == null) return await GetNoContentRespons("No week menu to create", req);

                    var weekMenu = _mapper.Map<WeekMenu>(requestBody);
                    weekMenu.LastModified = DateTime.UtcNow;
                    weekMenu.IsActive = true;

                    if (string.IsNullOrWhiteSpace(weekMenu.Name))
                        weekMenu.Name = $"Uke {weekMenu.WeekNumber} {weekMenu.Year}";

                    if (weekMenu.DailyMeals == null)
                        weekMenu.DailyMeals = new List<DailyMeal>();

                    var addRes = await _repository.Insert(weekMenu);
                    if (addRes == null)
                    {
                        _logger.LogWarning("Could not create week menu");
                        return await GetErroRespons("Could not create week menu", req);
                    }

                    await response.WriteAsJsonAsync(_mapper.Map<WeekMenuModel>(addRes));
                    return response;
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<WeekMenuModel>();
                    if (requestBody == null) return await GetNoContentRespons("No week menu to update", req);

                    var weekMenu = _mapper.Map<WeekMenu>(requestBody);
                    weekMenu.LastModified = DateTime.UtcNow;

                    var updateRes = await _repository.Update(weekMenu);
                    if (updateRes == null)
                        return await GetErroRespons("Could not update week menu", req);

                    await response.WriteAsJsonAsync(_mapper.Map<WeekMenuModel>(updateRes));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of weekmenus, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("weekmenu")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "weekmenu/{id}")] HttpRequestData req, string id)
        {
            try
            {
                if (req.Method == "GET")
                {
                    var result = await _repository.Get(id);
                    if (result == null)
                    {
                        _logger.LogInformation($"Could not find week menu with id {id}");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    var weekMenuModel = _mapper.Map<WeekMenuModel>(result);
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(weekMenuModel);
                    return response;
                }
                else if (req.Method == "DELETE")
                {
                    // Soft delete: mark inactive rather than remove from Firestore
                    var item = await _repository.Get(id);
                    if (item == null)
                    {
                        _logger.LogWarning($"Could not find week menu with id {id} for soft delete");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    item.IsActive = false;
                    item.LastModified = DateTime.UtcNow;

                    var updatedItem = await _repository.Update(item);
                    if (updatedItem == null)
                        return await GetErroRespons($"Could not soft-delete week menu with id {id}", req);

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(_mapper.Map<WeekMenuModel>(updatedItem));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of weekmenu/{id}, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("weekmenubyweek")]
        public async Task<HttpResponseData> RunByWeek([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weekmenu/week/{weekNumber}/year/{year}")] HttpRequestData req, int weekNumber, int year)
        {
            try
            {
                var all = await _repository.Get();
                if (all == null)
                    return req.CreateResponse(HttpStatusCode.NotFound);

                var match = all.FirstOrDefault(w => w.WeekNumber == weekNumber && w.Year == year && w.IsActive);
                if (match == null)
                {
                    _logger.LogInformation($"No active week menu found for week {weekNumber} year {year}");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(_mapper.Map<WeekMenuModel>(match));
                return response;
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong looking up week menu for week {weekNumber} year {year}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("weekmenugenerateshoppinglist")]
        public async Task<HttpResponseData> RunGenerateShoppingList([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekmenu/{id}/generate-shoppinglist")] HttpRequestData req, string id)
        {
            try
            {
                var weekMenu = await _repository.Get(id);
                if (weekMenu == null)
                {
                    _logger.LogInformation($"Could not find week menu with id {id} for shopping list generation");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                // Build recipe lookup dictionary in a single repository call
                var allRecipes = await _mealRepository.Get();
                var recipeDict = allRecipes?
                    .Where(r => r.IsActive)
                    .ToDictionary(r => r.Id, r => r) ?? new Dictionary<string, MealRecipe>();

                // Aggregate ingredients: key = ShopItemId, value = (totalQuantity, shopItemName)
                var aggregated = new Dictionary<string, (double Quantity, string ShopItemName)>();

                foreach (var daily in weekMenu.DailyMeals ?? Enumerable.Empty<DailyMeal>())
                {
                    IEnumerable<MealIngredient> ingredients;

                    if (daily.CustomIngredients != null && daily.CustomIngredients.Any())
                    {
                        ingredients = daily.CustomIngredients;
                    }
                    else if (!string.IsNullOrEmpty(daily.MealRecipeId) && recipeDict.TryGetValue(daily.MealRecipeId, out var recipe))
                    {
                        ingredients = recipe.Ingredients ?? Enumerable.Empty<MealIngredient>();
                    }
                    else
                    {
                        continue;
                    }

                    foreach (var ingredient in ingredients)
                    {
                        if (string.IsNullOrEmpty(ingredient.ShopItemId)) continue;

                        if (aggregated.TryGetValue(ingredient.ShopItemId, out var existing))
                            aggregated[ingredient.ShopItemId] = (existing.Quantity + ingredient.Quantity, existing.ShopItemName);
                        else
                            aggregated[ingredient.ShopItemId] = (ingredient.Quantity, ingredient.ShopItemName ?? string.Empty);
                    }
                }

                var shoppingItems = aggregated.Select(kvp => new ShoppingListItemModel
                {
                    Varen = new ShopItemModel { Id = kvp.Key, Name = kvp.Value.ShopItemName },
                    Mengde = (int)Math.Ceiling(kvp.Value.Quantity),
                    IsDone = false
                }).ToList();

                var shoppingList = new ShoppingListModel
                {
                    Name = $"Ukemeny Uke {weekMenu.WeekNumber} {weekMenu.Year}",
                    ShoppingItems = shoppingItems,
                    LastModified = DateTime.UtcNow
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(shoppingList);
                return response;
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong generating shopping list for week menu {id}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
    }
}
