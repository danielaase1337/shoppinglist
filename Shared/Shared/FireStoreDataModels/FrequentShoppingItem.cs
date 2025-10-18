using Google.Cloud.Firestore;
using Shared.BaseModels;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class FrequentShoppingItem : EntityBase
    {
        [FirestoreProperty]
        public ShopItem Varen { get; set; } = new ShopItem();
        
        [FirestoreProperty]
        public int StandardMengde { get; set; } = 1;
    }
}