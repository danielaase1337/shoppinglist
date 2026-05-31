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
        // Named constants for collection keys — informational; GetCollectionKey() derives these via convention
        public const string FamilyProfiles = "familyprofiles";
        public const string PortionRules = "portionrules";
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
            // Legacy collection names that don't follow the naming convention — preserve for backward compatibility
            if (toTypeGet == typeof(Shop))
                return "shopcollection";
            if (toTypeGet == typeof(ItemCategory))
                return "itemcategories"; // irregular plural; convention would give "itemcategorys"

            // Convention: lowercase type name + "s" (e.g. ShoppingList → shoppinglists, MealRecipe → mealrecipes)
            return toTypeGet.Name.ToLower() + "s";
        }
    }
}
