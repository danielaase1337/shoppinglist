using System.Collections.Generic;

namespace Shared.HandlelisteModels
{
    public class DailyMealModel
    {
        public System.DayOfWeek Day { get; set; }
        public string MealRecipeId { get; set; }
        public string MealRecipeName { get; set; }
        public bool IsSuggested { get; set; }

        // #74 — Marks that this day's meal has been cooked and consumed (triggers inventory deduction)
        public bool IsConsumed { get; set; }

        public ICollection<MealIngredientModel> CustomIngredients { get; set; }

        public DailyMealModel()
        {
            CustomIngredients = new List<MealIngredientModel>();
            IsSuggested = false;
            IsConsumed = false;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(MealRecipeId);
        }
    }
}

