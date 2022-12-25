using Shared.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.HandlelisteModels
{
    public class ShopModel : EntityBase
    {
        public ICollection<ShelfModel> ShelfsInShop { get; set; }

       
    }
}
