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
        public MealType MealType { get; set; }

        [FirestoreProperty]
        public int PopularityScore { get; set; }

        [FirestoreProperty]
        public DateTime? LastUsed { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; }

        [FirestoreProperty]
        public bool IsFresh { get; set; }

        [FirestoreProperty]
        public int? PrepTimeMinutes { get; set; }

        [FirestoreProperty]
        public MealEffort Effort { get; set; }

        [FirestoreProperty]
        public int BasePortions { get; set; }

        [FirestoreProperty]
        public ICollection<MealIngredient> Ingredients { get; set; }

        public MealRecipe()
        {
            Ingredients = new List<MealIngredient>();
            IsActive = true;
            PopularityScore = 0;
            BasePortions = 4;
            MealType = MealType.FreshCook;
            Effort = MealEffort.Normal;
        }
    }
}

