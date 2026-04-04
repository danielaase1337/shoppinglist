using Google.Cloud.Firestore;
using Shared.BaseModels;
using System;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class InventoryItem : EntityBase
    {
        [FirestoreProperty]
        public string ShopItemId { get; set; }

        [FirestoreProperty]
        public string ShopItemName { get; set; }

        [FirestoreProperty]
        public double QuantityInStock { get; set; }

        [FirestoreProperty]
        public MealUnit Unit { get; set; }

        [FirestoreProperty]
        public double LowerThreshold { get; set; }

        [FirestoreProperty]
        public DateTime? LastUpdated { get; set; }

        [FirestoreProperty]
        public string ItemCategory { get; set; }

        [FirestoreProperty]
        public string SourceMealRecipeId { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; }

        public InventoryItem()
        {
            QuantityInStock = 0;
            LowerThreshold = 0;
            IsActive = true;
        }
    }
}
