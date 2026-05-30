using Google.Cloud.Firestore;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class MealIngredient
    {
        [FirestoreProperty]
        public string ShopItemId { get; set; }

        [FirestoreProperty]
        public string ShopItemName { get; set; }

        [FirestoreProperty]
        public double Quantity { get; set; }

        [FirestoreProperty]
        public MealUnit Unit { get; set; }

        [FirestoreProperty]
        public bool IsOptional { get; set; }

        [FirestoreProperty]
        public bool IsFresh { get; set; }

        [FirestoreProperty]
        public bool IsBasic { get; set; }

        public MealIngredient()
        {
            Quantity = 1;
            IsOptional = false;
            IsFresh = false;
            IsBasic = false;
        }
    }
}

