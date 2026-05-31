using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorApp.Client.Common
{
    public enum ShoppingListKeysEnum
    {
        None = 0,
        ShoppingList = 1,
        ShoppingLists = 2,
        Shop = 3,
        Shops = 4,
        ShopItem = 5,
        shopItems = 6,
        itemcategory = 7,
        itemcategorys = 8,
        FrequentShoppingList = 9,
        FrequentShoppingLists = 10,
        MealRecipes = 11,
        MealRecipe = 12,
        WeekMenus = 13,
        WeekMenu = 14,
        InventoryItems = 15,
        InventoryItem = 16,
        InventoryItemsAdjust = 17,
        FamilyProfiles = 18,
        FamilyProfile = 19,
        PortionRules = 20,
        PortionRule = 21,
        // #74 — Week menu action endpoints
        WeekMenuConsume = 22,
        WeekMenuSwap = 23,
        // #81 — Undo consume
        WeekMenuUnconsume = 24,
        // #72a — Suggest a 7-day meal plan
        WeekMenuSuggest = 25
    }
}
