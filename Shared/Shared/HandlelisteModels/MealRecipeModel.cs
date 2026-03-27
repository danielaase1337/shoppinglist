using Shared.BaseModels;
using System;
using System.Collections.Generic;

namespace Shared.HandlelisteModels
{
    public class MealRecipeModel : EntityBase
    {
        public MealCategory Category { get; set; }
        public int PopularityScore { get; set; }
        public DateTime? LastUsed { get; set; }
        public ICollection<MealIngredientModel> Ingredients { get; set; }
        public bool IsActive { get; set; }

        public MealRecipeModel()
        {
            Ingredients = new List<MealIngredientModel>();
            IsActive = true;
            PopularityScore = 0;
        }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}
