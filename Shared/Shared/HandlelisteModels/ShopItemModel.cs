using Shared.HandlelisteModels;
using Shared.BaseModels;

namespace Shared.HandlelisteModels
{
    public class ShopItemModel : EntityBase
    {

        public string Unit { get; set; } = "stk";
        public ItemCategoryModel ItemCategory { get; set; } = new ItemCategoryModel();
        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(Name)
                && ItemCategory.IsValid();
        }
    }
}
