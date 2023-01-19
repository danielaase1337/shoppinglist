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
            if (json == null) throw new NullReferenceException("Fant ikke googl cred");

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
            if (toTypeGet == typeof(ShopItem))
                return "shopitems";
            if (toTypeGet == typeof(ItemCategory))
                return "itemcategories";
            if (toTypeGet == typeof(ShoppingList))
                return "shoppinglists";
            if (toTypeGet == typeof(Shop))
                return "shopcollection";
            return "misc";
        }
    }
}
