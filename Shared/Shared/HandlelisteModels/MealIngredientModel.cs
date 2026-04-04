namespace Shared.HandlelisteModels
{
    public class MealIngredientModel
    {
        public string ShopItemId { get; set; }
        public string ShopItemName { get; set; }
        public double Quantity { get; set; }
        public MealUnit Unit { get; set; }
        public bool IsOptional { get; set; }
        public bool IsFresh { get; set; }
        public bool IsBasic { get; set; }

        public MealIngredientModel()
        {
            Quantity = 1;
            IsOptional = false;
            IsFresh = false;
            IsBasic = false;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ShopItemId) && !string.IsNullOrEmpty(ShopItemName) && Quantity > 0;
        }
    }
}

