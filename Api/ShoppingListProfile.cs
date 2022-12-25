using AutoMapper;
using Shared.HandlelisteModels;
using Shared.FireStoreDataModels;

namespace Api
{
    public class ShoppingListProfile : Profile
    {
        public ShoppingListProfile()
        {
            this.CreateMap<ShoppingList, ShoppingListModel>().ReverseMap();
            this.CreateMap<ShoppingListItem, ShoppingListItemModel>().ReverseMap();
            this.CreateMap<ShopItem, ShopItemModel>().ReverseMap();
            this.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            this.CreateMap<Shelf, ShelfModel>().ReverseMap();
        }
    }
}
