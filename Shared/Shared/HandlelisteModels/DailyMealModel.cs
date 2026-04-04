using System.Collections.Generic;

namespace Shared.HandlelisteModels
{
    public class DailyMealModel
    {
        public System.DayOfWeek Day { get; set; }
        public string MealRecipeId { get; set; }
        public bool IsSuggested { get; set; }
        public ICollection<MealIngredientModel> CustomIngredients { get; set; }

        public DailyMealModel()
        {
            CustomIngredients = new List<MealIngredientModel>();
            IsSuggested = false;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(MealRecipeId);
        }
    }
}

