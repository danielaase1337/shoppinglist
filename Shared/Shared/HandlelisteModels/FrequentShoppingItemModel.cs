using Shared.BaseModels;

namespace Shared.HandlelisteModels
{
    public class FrequentShoppingItemModel : EntityBase
    {
        public ShopItemModel Varen { get; set; } = new ShopItemModel();
        public int StandardMengde { get; set; } = 1;
    }
}