using Shared.BaseModels;

namespace Shared.HandlelisteModels

{
    public class ShelfModel: EntityBase
    {
        public ICollection<ItemCategoryModel> ItemCateogries { get; set; } = new List<ItemCategoryModel>(); 
        public int SortIndex { get; set; } //for å konfigurere en butikk
    }
}
