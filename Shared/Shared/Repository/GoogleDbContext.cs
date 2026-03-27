using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Shared.FireStoreDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Repository
{
    public class GoogleDbContext : IGoogleDbContext
    {
        readonly string _projectId = "supergnisten-shoppinglist";
        public string CollectionKey { get; set; }

        public GoogleDbContext()
        {
           var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");
            if(Path.IsPathFullyQualified(json))//This is to check if the env variable is a path to a file and then read the json. In prod this will read directly the credentials
            {
                json = File.ReadAllText(json);
            }
            if (json == null) throw new NullReferenceException("Fant ikke googl cred");
           
    
           
           Console.WriteLine("Googel cred is found");
            DB = new FirestoreDbBuilder
            {
                ProjectId = _projectId,
                JsonCredentials = json
            }.Build();
        }
        public CollectionReference Collection { get; set; }
        public FirestoreDb DB { get; private set; }

        public string GetCollectionKey(Type toTypeGet)
        {
            // Two legacy overrides where convention breaks:
            // Shop has a legacy collection name that predates the convention.
            // ItemCategory has an irregular plural (convention would produce "itemcategorys").
            if (toTypeGet == typeof(Shop))
                return "shopcollection";
            if (toTypeGet == typeof(ItemCategory))
                return "itemcategories";

            // Convention: TypeName.ToLower() + "s"
            // ShopItem → shopitems, ShoppingList → shoppinglists,
            // FrequentShoppingList → frequentshoppinglists, MealRecipe → mealrecipes,
            // WeekMenu → weekmenus, DailyMeal → dailymeals, etc.
            return toTypeGet.Name.ToLower() + "s";
        }
    }
}
