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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Api.Controllers
{
    // Request body for ConsumeMeal endpoint (#74)
    public class ConsumeMealRequest
    {
        public int DayOfWeek { get; set; }
        public string MealRecipeId { get; set; }
    }

    // Request body for SwapMeal endpoint (#74)
    public class SwapMealRequest
    {
        public int DayOfWeek { get; set; }
        public string NewMealRecipeId { get; set; }
    }

    public class WeekMenuController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<WeekMenu> _repository;
        private readonly IGenericRepository<MealRecipe> _mealRepository;
        private readonly IGenericRepository<InventoryItem> _inventoryRepository;
        private readonly IGenericRepository<ShopItem> _shopItemRepository;
        private readonly IMapper _mapper;

        public WeekMenuController(
            ILoggerFactory loggerFactory,
            IGenericRepository<WeekMenu> repository,
            IGenericRepository<MealRecipe> mealRepository,
            IGenericRepository<InventoryItem> inventoryRepository,
            IGenericRepository<ShopItem> shopItemRepository,
            IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<WeekMenuController>();
            _repository = repository;
            _mealRepository = mealRepository;
            _inventoryRepository = inventoryRepository;
            _shopItemRepository = shopItemRepository;
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

        // #72a — Suggest a balanced 7-day meal plan based on recent history and category targets
        [Function("weekmenu-suggest")]
        public async Task<HttpResponseData> SuggestMenu(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weekmenu/suggest")] HttpRequestData req)
        {
            try
            {
                // Parse optional weekNumber / year from query string; default to current ISO week
                var qs = HttpUtility.ParseQueryString(req.Url.Query);
                var today = DateTime.UtcNow;
                int currentWeek = ISOWeek.GetWeekOfYear(today);
                int currentYear = today.Year;

                if (!int.TryParse(qs["weekNumber"], out int targetWeek)) targetWeek = currentWeek;
                if (!int.TryParse(qs["year"], out int targetYear)) targetYear = currentYear;

                // Collect the two preceding ISO week numbers (handles year boundary)
                var recentWeeks = GetPreviousWeeks(targetWeek, targetYear, 2);

                // Fetch all week menus and filter to the history window
                var allMenus = await _repository.Get() ?? new List<WeekMenu>();
                var recentMenus = allMenus
                    .Where(m => m.IsActive && recentWeeks.Any(w => w.Week == m.WeekNumber && w.Year == m.Year))
                    .ToList();

                // Collect recipe IDs used in the history window
                var recentlyUsedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var menu in recentMenus)
                {
                    foreach (var daily in menu.DailyMeals ?? Enumerable.Empty<DailyMeal>())
                    {
                        if (!string.IsNullOrEmpty(daily.MealRecipeId))
                            recentlyUsedIds.Add(daily.MealRecipeId);
                    }
                }

                // Fetch all active meals; guard against null Id (legacy Firestore documents)
                var allMeals = await _mealRepository.Get() ?? new List<MealRecipe>();
                var activeMeals = allMeals
                    .Where(m => m.IsActive && m.Id != null)
                    .ToList();

                if (!activeMeals.Any())
                {
                    var emptyResp = req.CreateResponse(HttpStatusCode.OK);
                    await emptyResp.WriteAsJsonAsync(new List<MealRecipeModel>());
                    return emptyResp;
                }

                // Celebration meals are always eligible regardless of recent history
                var eligible = activeMeals
                    .Where(m => m.Category == Shared.FireStoreDataModels.MealCategory.Celebration
                                || !recentlyUsedIds.Contains(m.Id))
                    .OrderByDescending(m => m.PopularityScore)
                    .ToList();

                // Target category distribution for a 7-day plan
                var targets = new List<(Shared.FireStoreDataModels.MealCategory Category, int Count)>
                {
                    (Shared.FireStoreDataModels.MealCategory.Fish,     2),
                    (Shared.FireStoreDataModels.MealCategory.Chicken,  1),
                    (Shared.FireStoreDataModels.MealCategory.Pasta,    1),
                    (Shared.FireStoreDataModels.MealCategory.KidsLike, 1),
                    (Shared.FireStoreDataModels.MealCategory.Meat,     1)
                    // Remaining slot filled from any category by popularity
                };

                var suggestions = new List<MealRecipe>();
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Fill targeted category slots (highest-popularity within each category wins)
                foreach (var (cat, count) in targets)
                {
                    var picks = eligible
                        .Where(m => m.Category == cat && !used.Contains(m.Id))
                        .Take(count);
                    foreach (var pick in picks)
                    {
                        suggestions.Add(pick);
                        used.Add(pick.Id);
                    }
                }

                // Fill remaining slots up to 7 from highest-popularity eligible meals
                var remaining = eligible
                    .Where(m => !used.Contains(m.Id))
                    .Take(7 - suggestions.Count);
                foreach (var r in remaining)
                {
                    suggestions.Add(r);
                    used.Add(r.Id);
                }

                // Fallback: if the eligible pool is exhausted, allow recently-used meals to fill the plan
                if (suggestions.Count < 7)
                {
                    var fallback = activeMeals
                        .Where(m => !used.Contains(m.Id))
                        .OrderByDescending(m => m.PopularityScore)
                        .Take(7 - suggestions.Count);
                    suggestions.AddRange(fallback);
                }

                var models = _mapper.Map<List<MealRecipeModel>>(suggestions);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(models);
                return response;
            }
            catch (Exception e)
            {
                var msg = "Something went wrong generating menu suggestions";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        /// <summary>Returns the <paramref name="count"/> ISO weeks immediately before the given week/year pair.</summary>
        private static List<(int Week, int Year)> GetPreviousWeeks(int weekNumber, int year, int count)
        {
            var result = new List<(int, int)>();
            int w = weekNumber;
            int y = year;
            for (int i = 0; i < count; i++)
            {
                w--;
                if (w < 1)
                {
                    y--;
                    w = ISOWeek.GetWeeksInYear(y);
                }
                result.Add((w, y));
            }
            return result;
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
                    .Where(r => r.IsActive && r.Id != null)
                    .ToDictionary(r => r.Id, r => r) ?? new Dictionary<string, MealRecipe>();

                // Build ShopItem lookup so IsBasic/StockBehaviour/StandardPurchase fields survive into the result
                var allShopItems = await _shopItemRepository.Get();
                var shopItemDict = allShopItems?
                    .Where(s => s.Id != null)
                    .ToDictionary(s => s.Id, s => s) ?? new Dictionary<string, ShopItem>();

                // Aggregate ingredients: key = ShopItemId, value = (totalQuantity, shopItemName, shopItem, isBasic, unit, unitMismatch)
                // UnitMismatch=true when the same ShopItem appears with different MealUnits across meals — in that case
                // we cannot safely convert to packages and fall back to Math.Ceiling.
                var aggregated = new Dictionary<string, (double Quantity, string ShopItemName, ShopItem ShopItem, bool IsBasic, MealUnit Unit, bool UnitMismatch)>();

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

                        shopItemDict.TryGetValue(ingredient.ShopItemId, out var shopItem);
                        if (aggregated.TryGetValue(ingredient.ShopItemId, out var existing))
                        {
                            var mismatch = existing.UnitMismatch || existing.Unit != ingredient.Unit;
                            aggregated[ingredient.ShopItemId] = (
                                existing.Quantity + ingredient.Quantity,
                                string.IsNullOrEmpty(existing.ShopItemName) ? ingredient.ShopItemName ?? string.Empty : existing.ShopItemName,
                                existing.ShopItem ?? shopItem,
                                existing.IsBasic || ingredient.IsBasic,
                                existing.Unit,
                                mismatch);
                        }
                        else
                        {
                            aggregated[ingredient.ShopItemId] = (
                                ingredient.Quantity,
                                ingredient.ShopItemName ?? string.Empty,
                                shopItem,
                                ingredient.IsBasic,
                                ingredient.Unit,
                                false);
                        }
                    }
                }

                var shoppingItems = aggregated.Select(kvp =>
                {
                    // Use AutoMapper to carry all ShopItem fields (IsBasic, StockBehaviour, StandardPurchaseQuantity, StandardPurchaseUnit)
                    var varen = kvp.Value.ShopItem != null
                        ? _mapper.Map<ShopItemModel>(kvp.Value.ShopItem)
                        : new ShopItemModel
                        {
                            Id = kvp.Key,
                            Name = kvp.Value.ShopItemName,
                            IsBasic = kvp.Value.IsBasic
                        };

                    return new ShoppingListItemModel
                    {
                        Varen = varen,
                        Mengde = (int)Math.Ceiling(kvp.Value.Quantity),
                        IsDone = false,
                        IsMealSourced = true
                    };
                }).ToList();

                // #76 — Stock comparison: mark fully-covered items and reduce partially-covered demand.
                // Runs BEFORE package-size conversion so demand subtraction stays in the same unit dimension.
                // Cross-dimension case (e.g. inventory in Package, recipe in Gram): converts inventory
                // to recipe base units via StandardPurchaseQuantity before comparing.
                var allInventory = await _inventoryRepository.Get();
                var inventoryDict = allInventory?
                    .Where(i => i.IsActive && i.ShopItemId != null)
                    .GroupBy(i => i.ShopItemId)
                    .ToDictionary(g => g.Key, g => g.First())
                    ?? new Dictionary<string, InventoryItem>();

                foreach (var item in shoppingItems)
                {
                    if (item.Varen?.Id == null) continue;
                    if (!inventoryDict.TryGetValue(item.Varen.Id, out var inv)) continue;

                    bool hasAgg = aggregated.TryGetValue(item.Varen.Id, out var agg);

                    // Cross-dimension smart comparison:
                    // When inventory is stored in packages/units but the recipe demands weight or volume,
                    // multiply inventory count by StandardPurchaseQuantity to obtain recipe-compatible units.
                    // Example: inv=1 bag (Package), StandardPurchaseQuantity=1000, StandardPurchaseUnit="g",
                    //          recipe needs 400 g → effectiveStock = 1×1000 = 1000 g ≥ 400 g → covered.
                    if (hasAgg
                        && !agg.UnitMismatch
                        && item.Varen.StandardPurchaseQuantity > 0
                        && !string.IsNullOrEmpty(item.Varen.StandardPurchaseUnit)
                        && !agg.Unit.IsCompatibleWith(inv.Unit.ToNorwegian()))
                    {
                        double invCountBase = inv.Unit.NormalizeToBaseUnit(inv.QuantityInStock);
                        double packageInRecipeBase = MealUnitExtensions.NormalizePurchaseUnitToBase(
                            item.Varen.StandardPurchaseQuantity, item.Varen.StandardPurchaseUnit);

                        if (!double.IsNaN(invCountBase) && !double.IsNaN(packageInRecipeBase) && packageInRecipeBase > 0)
                        {
                            double stockInRecipeBase = invCountBase * packageInRecipeBase;
                            double demandInBase = agg.Unit.NormalizeToBaseUnit(agg.Quantity);

                            if (!double.IsNaN(demandInBase))
                            {
                                if (stockInRecipeBase >= demandInBase)
                                {
                                    item.IsLikelyNotNeeded = true;
                                    item.IsInventoryCovered = true;
                                }
                                else if (stockInRecipeBase > 0)
                                {
                                    // Partial: convert remaining demand back from base to agg.Unit
                                    double unitFactor = agg.Unit.NormalizeToBaseUnit(1);
                                    if (!double.IsNaN(unitFactor) && unitFactor > 0)
                                        item.Mengde = (int)Math.Ceiling((demandInBase - stockInRecipeBase) / unitFactor);
                                }
                                continue; // handled — skip same-dimension fallback
                            }
                        }
                    }

                    // Same-dimension (or unknown-unit) fallback: compare raw quantities directly.
                    var stock = inv.QuantityInStock;
                    if (stock >= item.Mengde)
                    {
                        item.IsLikelyNotNeeded = true;
                        item.IsInventoryCovered = true;
                    }
                    else if (stock > 0)
                    {
                        // Partially covered — reduce demand by available stock
                        item.Mengde = (int)Math.Ceiling((double)item.Mengde - stock);
                    }
                }

                // Mark IsBasic items as likely not needed — staples/spices user probably has in the cupboard
                foreach (var item in shoppingItems)
                {
                    if (item.Varen?.IsBasic == true)
                        item.IsLikelyNotNeeded = true;
                }

                // #76 Package-size calculation: convert stock-adjusted raw demand into package count.
                // Only applies when StandardPurchaseQuantity is set and units are compatible.
                // Falls back to the raw Math.Ceiling value already in Mengde for unknown/incompatible units.
                foreach (var item in shoppingItems)
                {
                    if (item.Varen?.Id == null) continue;
                    if (!aggregated.TryGetValue(item.Varen.Id, out var agg)) continue;
                    if (agg.UnitMismatch) continue;  // mixed units across meals — cannot reliably convert
                    if (item.Varen.StandardPurchaseQuantity <= 0 || string.IsNullOrEmpty(item.Varen.StandardPurchaseUnit)) continue;

                    var packages = MealUnitExtensions.CalculatePackagesNeeded(
                        item.Mengde, agg.Unit,
                        item.Varen.StandardPurchaseQuantity, item.Varen.StandardPurchaseUnit);

                    if (packages.HasValue)
                        item.Mengde = packages.Value;
                }

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

        // #74 — Mark a daily meal as consumed and deduct ingredients from inventory
        [Function("weekmenuconsume")]
        public async Task<HttpResponseData> ConsumeMeal([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "weekmenu/{weekMenuId}/consume")] HttpRequestData req, string weekMenuId)
        {
            try
            {
                var requestBody = await req.ReadFromJsonAsync<ConsumeMealRequest>();
                if (requestBody == null)
                    return await GetNoContentRespons("No consume request body", req);

                var weekMenu = await _repository.Get(weekMenuId);
                if (weekMenu == null)
                {
                    _logger.LogInformation($"Could not find week menu with id {weekMenuId} for consume");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var targetDay = (System.DayOfWeek)requestBody.DayOfWeek;
                var dailyMeal = weekMenu.DailyMeals?.FirstOrDefault(d => d.Day == targetDay);
                if (dailyMeal == null)
                {
                    _logger.LogInformation($"No daily meal found for day {targetDay} in week menu {weekMenuId}");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                dailyMeal.IsConsumed = true;
                weekMenu.LastModified = DateTime.UtcNow;

                var updatedMenu = await _repository.Update(weekMenu);
                if (updatedMenu == null)
                    return await GetErroRespons($"Could not update week menu {weekMenuId} after consume", req);

                // Deduct ingredients from inventory
                if (!string.IsNullOrEmpty(requestBody.MealRecipeId))
                {
                    var recipe = await _mealRepository.Get(requestBody.MealRecipeId);
                    if (recipe != null && recipe.Ingredients != null)
                    {
                        var allInventory = await _inventoryRepository.Get();

                        foreach (var ingredient in recipe.Ingredients)
                        {
                            if (string.IsNullOrEmpty(ingredient.ShopItemId)) continue;

                            var inventoryItem = allInventory?.FirstOrDefault(i => i.ShopItemId == ingredient.ShopItemId && i.IsActive);
                            if (inventoryItem == null) continue; // No inventory entry — skip, no crash

                            inventoryItem.QuantityInStock = Math.Max(0, inventoryItem.QuantityInStock - ingredient.Quantity);
                            inventoryItem.LastModified = DateTime.UtcNow;
                            await _inventoryRepository.Update(inventoryItem);
                        }
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(_mapper.Map<WeekMenuModel>(updatedMenu));
                return response;
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong consuming meal in week menu {weekMenuId}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        // #81 — Undo consume: mark meal unconsumed and restore ingredient quantities to inventory
        [Function("weekmenuunconsume")]
        public async Task<HttpResponseData> UnConsumeMeal([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "weekmenu/{weekMenuId}/unconsume")] HttpRequestData req, string weekMenuId)
        {
            try
            {
                var requestBody = await req.ReadFromJsonAsync<ConsumeMealRequest>();
                if (requestBody == null)
                    return await GetNoContentRespons("No unconsume request body", req);

                var weekMenu = await _repository.Get(weekMenuId);
                if (weekMenu == null)
                {
                    _logger.LogInformation($"Could not find week menu with id {weekMenuId} for unconsume");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var targetDay = (System.DayOfWeek)requestBody.DayOfWeek;
                var dailyMeal = weekMenu.DailyMeals?.FirstOrDefault(d => d.Day == targetDay);
                if (dailyMeal == null)
                {
                    _logger.LogInformation($"No daily meal found for day {targetDay} in week menu {weekMenuId}");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                dailyMeal.IsConsumed = false;
                weekMenu.LastModified = DateTime.UtcNow;

                var updatedMenu = await _repository.Update(weekMenu);
                if (updatedMenu == null)
                    return await GetErroRespons($"Could not update week menu {weekMenuId} after unconsume", req);

                // Restore ingredients to inventory (reverse the deduction)
                if (!string.IsNullOrEmpty(requestBody.MealRecipeId))
                {
                    var recipe = await _mealRepository.Get(requestBody.MealRecipeId);
                    if (recipe != null && recipe.Ingredients != null)
                    {
                        var allInventory = await _inventoryRepository.Get();

                        foreach (var ingredient in recipe.Ingredients)
                        {
                            if (string.IsNullOrEmpty(ingredient.ShopItemId)) continue;

                            var inventoryItem = allInventory?.FirstOrDefault(i => i.ShopItemId == ingredient.ShopItemId && i.IsActive);
                            if (inventoryItem == null) continue; // No inventory entry — skip, no crash

                            inventoryItem.QuantityInStock += ingredient.Quantity;
                            inventoryItem.LastModified = DateTime.UtcNow;
                            await _inventoryRepository.Update(inventoryItem);
                        }
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(_mapper.Map<WeekMenuModel>(updatedMenu));
                return response;
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong unconsuming meal in week menu {weekMenuId}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        // #74 — Swap the meal for a specific day without affecting inventory
        [Function("weekmenuswap")]
        public async Task<HttpResponseData> SwapMeal([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "weekmenu/{weekMenuId}/swap")] HttpRequestData req, string weekMenuId)
        {
            try
            {
                var requestBody = await req.ReadFromJsonAsync<SwapMealRequest>();
                if (requestBody == null)
                    return await GetNoContentRespons("No swap request body", req);

                var weekMenu = await _repository.Get(weekMenuId);
                if (weekMenu == null)
                {
                    _logger.LogInformation($"Could not find week menu with id {weekMenuId} for swap");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var targetDay = (System.DayOfWeek)requestBody.DayOfWeek;
                var dailyMeal = weekMenu.DailyMeals?.FirstOrDefault(d => d.Day == targetDay);
                if (dailyMeal == null)
                {
                    _logger.LogInformation($"No daily meal found for day {targetDay} in week menu {weekMenuId} — cannot swap");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                dailyMeal.MealRecipeId = requestBody.NewMealRecipeId;
                weekMenu.LastModified = DateTime.UtcNow;

                var updatedMenu = await _repository.Update(weekMenu);
                if (updatedMenu == null)
                    return await GetErroRespons($"Could not update week menu {weekMenuId} after swap", req);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(_mapper.Map<WeekMenuModel>(updatedMenu));
                return response;
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong swapping meal in week menu {weekMenuId}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
    }
}
