using Shared.HandlelisteModels;
using Shared.BaseModels;

namespace Shared.HandlelisteModels
{
    public class ShopItemModel : EntityBase
    {

        public string Unit { get; set; } = "stk";
        public ItemCategoryModel ItemCategory { get; set; } = new ItemCategoryModel();

        // #73 — Staple/basic item (e.g. milk, bread — always stocked)
        public bool IsBasic { get; set; }

        // #75 — Controls inventory tracking behaviour for this item
        public StockBehaviour StockBehaviour { get; set; } = StockBehaviour.Track;

        // #76 — Standard purchase unit size (e.g. carrots come in 1 kg bags)
        public double StandardPurchaseQuantity { get; set; } // 0 = not set

        public string StandardPurchaseUnit { get; set; } // e.g. "kg", "stk", "l" — null/empty = not set

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(Name)
                && ItemCategory.IsValid();
        }
    }
}
