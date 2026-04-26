using Shared.BaseModels;
using System;

namespace Shared.HandlelisteModels
{
    public class InventoryItemModel : EntityBase
    {
        public string ShopItemId { get; set; }
        public string ShopItemName { get; set; }
        public double QuantityInStock { get; set; }
        public MealUnit Unit { get; set; }
        public double LowerThreshold { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string ItemCategory { get; set; }
        public string SourceMealRecipeId { get; set; }
        public bool IsActive { get; set; }

        public InventoryItemModel()
        {
            QuantityInStock = 0;
            LowerThreshold = 0;
            IsActive = true;
        }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(ShopItemId) && !string.IsNullOrEmpty(ShopItemName);
        }
    }
}
