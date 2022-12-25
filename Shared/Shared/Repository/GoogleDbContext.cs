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
            //CollectionKey = collectionName;
            DB = FirestoreDb.Create(_projectId);
            //Collection = DB.Collection(CollectionKey);
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
