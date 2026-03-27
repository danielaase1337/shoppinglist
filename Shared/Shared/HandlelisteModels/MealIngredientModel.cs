using Shared.BaseModels;

namespace Shared.HandlelisteModels
{
    public class MealIngredientModel : EntityBase
    {
        public string MealRecipeId { get; set; }
        public ShopItemModel ShopItem { get; set; }
        public int StandardQuantity { get; set; }
        public bool IsOptional { get; set; }

        public MealIngredientModel()
        {
            StandardQuantity = 1;
            IsOptional = false;
            ShopItem = new ShopItemModel();
        }

        public override bool IsValid()
        {
            return ShopItem != null && ShopItem.IsValid() && StandardQuantity > 0;
        }
    }
}
