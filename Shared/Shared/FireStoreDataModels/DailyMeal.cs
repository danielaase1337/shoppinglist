using Google.Cloud.Firestore;
using Shared.BaseModels;
using System;
using System.Collections.Generic;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class DailyMeal : EntityBase
    {
        [FirestoreProperty]
        public string WeekMenuId { get; set; }
        
        [FirestoreProperty]
        public DayOfWeek DayOfWeek { get; set; }
        
        [FirestoreProperty]
        public MealRecipe MealRecipe { get; set; }
        
        [FirestoreProperty]
        public ICollection<MealIngredient> CustomIngredients { get; set; }

        public DailyMeal()
        {
            CustomIngredients = new List<MealIngredient>();
        }
    }
}
