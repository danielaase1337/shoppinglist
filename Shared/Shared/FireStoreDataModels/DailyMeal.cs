using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class DailyMeal
    {
        [FirestoreProperty]
        public System.DayOfWeek Day { get; set; }

        [FirestoreProperty]
        public string MealRecipeId { get; set; }

        [FirestoreProperty]
        public bool IsSuggested { get; set; }

        [FirestoreProperty]
        public ICollection<MealIngredient> CustomIngredients { get; set; }

        public DailyMeal()
        {
            CustomIngredients = new List<MealIngredient>();
            IsSuggested = false;
        }
    }
}

