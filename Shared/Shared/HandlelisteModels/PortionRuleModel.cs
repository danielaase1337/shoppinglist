using Shared.BaseModels;

namespace Shared.HandlelisteModels
{
    public class PortionRuleModel : EntityBase
    {
        public string ShopItemId { get; set; }
        public string ShopItemName { get; set; }  // denormalized for display
        public AgeGroup AgeGroup { get; set; }
        public double QuantityPerPerson { get; set; }
        public MealUnit Unit { get; set; }
        public bool IsActive { get; set; }

        public override bool IsValid() => !string.IsNullOrEmpty(ShopItemId) && QuantityPerPerson > 0;
    }
}
