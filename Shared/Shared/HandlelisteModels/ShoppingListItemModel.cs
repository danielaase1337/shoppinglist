using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.HandlelisteModels
{
    public class ShoppingListItemModel : ShoppingListBaseModel
    {
        public ShopItemModel Varen { get; set; } = new ShopItemModel(); 
        public int Mengde { get; set; } = 1;

        // #76 — Set by generate-shoppinglist when inventory fully covers this item's demand
        public bool IsLikelyNotNeeded { get; set; }
    }
}
