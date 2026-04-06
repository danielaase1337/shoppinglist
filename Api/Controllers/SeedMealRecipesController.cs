using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.FireStoreDataModels;
using Shared.Repository;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using FirestoreMealCategory = Shared.FireStoreDataModels.MealCategory;
using FirestoreMealType = Shared.FireStoreDataModels.MealType;
using FirestoreMealEffort = Shared.FireStoreDataModels.MealEffort;

namespace Api.Controllers
{
    /// <summary>
    /// One-time seeding endpoint: populates the Firestore "mealrecipes" collection
    /// with the canonical family dinner list if the collection is empty.
    ///
    /// Safe to call repeatedly — skips seeding if any records already exist.
    /// </summary>
    public class SeedMealRecipesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<MealRecipe> _repository;

        public SeedMealRecipesController(
            ILoggerFactory loggerFactory,
            IGenericRepository<MealRecipe> repository)
        {
            _logger = loggerFactory.CreateLogger<SeedMealRecipesController>();
            _repository = repository;
        }

        [Function("seed-meal-recipes")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tools/seed-meal-recipes")]
            HttpRequestData req)
        {
            try
            {
                var existing = await _repository.Get();
                if (existing != null && existing.Count > 0)
                {
                    _logger.LogInformation("seed-meal-recipes: collection already has {Count} records — skipping", existing.Count);
                    var skipResponse = req.CreateResponse(HttpStatusCode.OK);
                    await skipResponse.WriteAsJsonAsync(new
                    {
                        message = $"Already seeded. {existing.Count} meal recipes exist — no action taken.",
                        alreadySeeded = true,
                        count = existing.Count
                    });
                    return skipResponse;
                }

                var seeds = BuildSeedRecipes();
                int inserted = 0;

                foreach (var recipe in seeds)
                {
                    var result = await _repository.Insert(recipe);
                    if (result != null)
                        inserted++;
                    else
                        _logger.LogWarning("seed-meal-recipes: failed to insert {Name}", recipe.Name);
                }

                _logger.LogInformation("seed-meal-recipes: inserted {Count} meal recipes into Firestore", inserted);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = $"Seeded {inserted} meal recipes into Firestore.",
                    seeded = true,
                    count = inserted
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during meal recipe seeding");
                return await GetErroRespons(ex.Message, req);
            }
        }

        private static List<MealRecipe> BuildSeedRecipes() => new List<MealRecipe>
        {
            // KidsLike
            new MealRecipe { Name = "Pizza",                   Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 100, IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Taco",                    Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 90,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Pannekaker",              Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick,   PopularityScore = 77,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Pølse og potetmos",       Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick,   PopularityScore = 65,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Grøt",                    Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick,   PopularityScore = 62,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Hamburger",               Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 58,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kyllingnuggets",          Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick,   PopularityScore = 56,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fiskeburger",             Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick,   PopularityScore = 52,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Favaffel",                Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 50,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Falafel",                 Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 44,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fiskepinner",             Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.Frozen,    Effort = FirestoreMealEffort.Quick,   PopularityScore = 41,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Pølsegnocchi",            Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Quick,   PopularityScore = 42,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Pølseform med makaroni",  Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 40,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Nachos",                  Category = FirestoreMealCategory.KidsLike,    MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 36,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

            // Fish
            new MealRecipe { Name = "Laks",                    Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 85,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fiskeboller",             Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 80,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fiskegrateng",            Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 80,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fiskekaker",              Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 60,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Salmalaks",               Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 54,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Laks i pita",             Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 47,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Hvit fisk",               Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 46,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fiskesuppe",              Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 42,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Torsk",                   Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 40,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fiskepakke",              Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 38,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kveite",                  Category = FirestoreMealCategory.Fish,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 35,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

            // Meat
            new MealRecipe { Name = "Lapskaus",                Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 78,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kjøttkaker",              Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 74,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Biff",                    Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 52,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Spareribs",               Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 50,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Bulgogi",                 Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 48,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kjøttboller",             Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 44,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Fårikål",                 Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 37,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Raspeballer",             Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 40,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Vossakorv",               Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 40,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Benløse fugler",          Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 36,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Nakkekoteletter",         Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 36,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Finnebiff",               Category = FirestoreMealCategory.Meat,        MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 35,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

            // Vegetarian
            new MealRecipe { Name = "Tomatsuppe",              Category = FirestoreMealCategory.Vegetarian,  MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 72,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Linsegryte",              Category = FirestoreMealCategory.Vegetarian,  MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 63,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Marokkansk bønnegryte",   Category = FirestoreMealCategory.Vegetarian,  MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 43,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Veggisburger",            Category = FirestoreMealCategory.Vegetarian,  MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 42,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Gulrotsuppe",             Category = FirestoreMealCategory.Vegetarian,  MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 38,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Quinoaburger",            Category = FirestoreMealCategory.Vegetarian,  MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 35,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

            // Chicken
            new MealRecipe { Name = "Kylling Gong Bao",        Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 68,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Tikka masala",            Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 45,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kyllingform",             Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 46,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kyllingsuppe",            Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 44,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kyllinglår",              Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 40,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kyllingklubber",          Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 40,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Hønsefrikassé",           Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 35,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Kalkun",                  Category = FirestoreMealCategory.Chicken,     MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 34,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

            // Pasta
            new MealRecipe { Name = "Lasagne",                 Category = FirestoreMealCategory.Pasta,       MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 75,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Spagetti og kjøttsaus",   Category = FirestoreMealCategory.Pasta,       MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 55,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Pastaform",               Category = FirestoreMealCategory.Pasta,       MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 48,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "One pot pasta",           Category = FirestoreMealCategory.Pasta,       MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 35,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

            // Celebration
            new MealRecipe { Name = "Pinnekjøtt",              Category = FirestoreMealCategory.Celebration, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 38,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Ribbe",                   Category = FirestoreMealCategory.Celebration, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Weekend, PopularityScore = 38,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Medisterkaker",           Category = FirestoreMealCategory.Celebration, MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 36,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },

            // Other
            new MealRecipe { Name = "Wok",                     Category = FirestoreMealCategory.Other,       MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 38,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Enchiladas",              Category = FirestoreMealCategory.Other,       MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 38,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
            new MealRecipe { Name = "Drunken noodles",         Category = FirestoreMealCategory.Other,       MealType = FirestoreMealType.FreshCook, Effort = FirestoreMealEffort.Normal,  PopularityScore = 34,  IsActive = true, BasePortions = 4, LastModified = DateTime.UtcNow },
        };
    }
}
