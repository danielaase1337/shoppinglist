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

    }
}
