using Google.Cloud.Firestore;
using Shared.BaseModels;
using System.Collections.Generic;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class FrequentShoppingList : EntityBase
    {
        [FirestoreProperty]
        public string Description { get; set; } = string.Empty;
        
        [FirestoreProperty]
        public ICollection<FrequentShoppingItem> Items { get; set; } = new List<FrequentShoppingItem>();
    }
}