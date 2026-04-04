using Google.Cloud.Firestore;
using Shared.BaseModels;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class PortionRule : EntityBase
    {
        [FirestoreProperty]
        public string ShopItemId { get; set; }

        [FirestoreProperty]
        public AgeGroup AgeGroup { get; set; }

        [FirestoreProperty]
        public double QuantityPerPerson { get; set; }

        [FirestoreProperty]
        public MealUnit Unit { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; }

        public PortionRule()
        {
            IsActive = true;
        }
    }
}
