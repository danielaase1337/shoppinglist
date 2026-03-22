using Shared.BaseModels;
using System;
using System.Collections.Generic;

namespace Shared.HandlelisteModels
{
    public class DailyMealModel : EntityBase
    {
        public string WeekMenuId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public MealRecipeModel MealRecipe { get; set; }
        public ICollection<MealIngredientModel> CustomIngredients { get; set; }

        public DailyMealModel()
        {
            CustomIngredients = new List<MealIngredientModel>();
            MealRecipe = new MealRecipeModel();
        }

        public override bool IsValid()
        {
            return MealRecipe != null && MealRecipe.IsValid();
        }
    }
}
