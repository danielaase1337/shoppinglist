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
            this.CreateMap<Shop, ShopModel>().ReverseMap();
            
            // Frequent Shopping Lists mappings
            this.CreateMap<FrequentShoppingList, FrequentShoppingListModel>().ReverseMap();
            this.CreateMap<FrequentShoppingItem, FrequentShoppingItemModel>().ReverseMap();
            
            // Meal Planning mappings
            this.CreateMap<MealRecipe, MealRecipeModel>().ReverseMap();
            this.CreateMap<MealIngredient, MealIngredientModel>().ReverseMap();
            this.CreateMap<WeekMenu, WeekMenuModel>().ReverseMap();
            this.CreateMap<DailyMeal, DailyMealModel>().ReverseMap();
            this.CreateMap<InventoryItem, InventoryItemModel>().ReverseMap();
        }
    }
}
