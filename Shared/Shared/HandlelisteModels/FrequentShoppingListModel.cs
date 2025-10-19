using Shared.BaseModels;
using System.Collections.Generic;

namespace Shared.HandlelisteModels
{
    public class FrequentShoppingListModel : EntityBase
    {
        public string Description { get; set; } = string.Empty;
        public ICollection<FrequentShoppingItemModel> Items { get; set; } = new List<FrequentShoppingItemModel>();
    }
}