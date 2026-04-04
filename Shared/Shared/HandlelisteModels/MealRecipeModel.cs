using Shared.BaseModels;
using System;
using System.Collections.Generic;

namespace Shared.HandlelisteModels
{
    public class MealRecipeModel : EntityBase
    {
        public MealCategory Category { get; set; }
        public MealType MealType { get; set; }
        public int PopularityScore { get; set; }
        public DateTime? LastUsed { get; set; }
        public bool IsActive { get; set; }
        public bool IsFresh { get; set; }
        public int? PrepTimeMinutes { get; set; }
        public MealEffort Effort { get; set; }
        public int BasePortions { get; set; }
        public ICollection<MealIngredientModel> Ingredients { get; set; }

        public MealRecipeModel()
        {
            Ingredients = new List<MealIngredientModel>();
            IsActive = true;
            PopularityScore = 0;
            BasePortions = 4;
            MealType = MealType.FreshCook;
            Effort = MealEffort.Normal;
        }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}

