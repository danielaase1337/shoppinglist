using Google.Cloud.Firestore;
using Shared.BaseModels;
using System;
using System.Collections.Generic;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class MealRecipe : EntityBase
    {
        [FirestoreProperty]
        public MealCategory Category { get; set; }
        
        [FirestoreProperty]
        public int PopularityScore { get; set; }
        
        [FirestoreProperty]
        public DateTime? LastUsed { get; set; }
        
        [FirestoreProperty]
        public ICollection<MealIngredient> Ingredients { get; set; }
        
        [FirestoreProperty]
        public bool IsActive { get; set; }

        public MealRecipe()
        {
            Ingredients = new List<MealIngredient>();
            IsActive = true;
            PopularityScore = 0;
        }
    }
}
