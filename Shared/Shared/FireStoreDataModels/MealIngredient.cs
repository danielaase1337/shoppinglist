using Google.Cloud.Firestore;
using Shared.BaseModels;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class MealIngredient : EntityBase
    {
        [FirestoreProperty]
        public string MealRecipeId { get; set; }
        
        [FirestoreProperty]
        public ShopItem ShopItem { get; set; }
        
        [FirestoreProperty]
        public int StandardQuantity { get; set; }
        
        [FirestoreProperty]
        public bool IsOptional { get; set; }

        public MealIngredient()
        {
            StandardQuantity = 1;
            IsOptional = false;
        }
    }
}
