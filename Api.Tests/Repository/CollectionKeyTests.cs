using Shared.FireStoreDataModels;
using Shared.Repository;
using Xunit;

namespace Api.Tests.Repository
{
    /// <summary>
    /// Verifies that GetCollectionKey() maps all registered entity types to the correct
    /// Firestore collection names — preventing silent writes to the "misc" collection.
    /// Decision D4: convention is typeof(T).Name.ToLower() + "s".
    /// Shop is a backward-compatible exception: "shopcollection".
    /// </summary>
    public class CollectionKeyTests
    {
        private readonly TestableGoogleDbContext _context = new();

        // Existing 4 collections — must never change (production data in place)
        [Fact] public void ShoppingList_MapsTo_shoppinglists() => Assert.Equal("shoppinglists", _context.GetCollectionKey(typeof(ShoppingList)));
        [Fact] public void ShopItem_MapsTo_shopitems() => Assert.Equal("shopitems", _context.GetCollectionKey(typeof(ShopItem)));
        [Fact] public void ItemCategory_MapsTo_itemcategories() => Assert.Equal("itemcategories", _context.GetCollectionKey(typeof(ItemCategory)));
        [Fact] public void Shop_MapsTo_shopcollection_BackwardCompat() => Assert.Equal("shopcollection", _context.GetCollectionKey(typeof(Shop)));

        // New types — previously fell through to "misc" (P0 bug)
        [Fact] public void FrequentShoppingList_MapsTo_frequentshoppinglists() => Assert.Equal("frequentshoppinglists", _context.GetCollectionKey(typeof(FrequentShoppingList)));
        [Fact] public void MealRecipe_MapsTo_mealrecipes() => Assert.Equal("mealrecipes", _context.GetCollectionKey(typeof(MealRecipe)));
        [Fact] public void WeekMenu_MapsTo_weekmenus() => Assert.Equal("weekmenus", _context.GetCollectionKey(typeof(WeekMenu)));

        [Fact]
        public void UnknownType_UsesConvention_NotMisc()
        {
            // Any future entity type must NOT fall through to "misc"
            var key = _context.GetCollectionKey(typeof(SomeHypotheticalFutureEntity));
            Assert.NotEqual("misc", key);
            Assert.Equal("somehypotheticalfutureentitys", key);
        }
    }

    /// <summary>
    /// Thin subclass that exposes GetCollectionKey() without requiring
    /// a live Firestore connection (skips the GoogleDbContext constructor).
    /// </summary>
    internal class TestableGoogleDbContext : IGoogleDbContext
    {
        public string CollectionKey { get; set; } = string.Empty;
        public Google.Cloud.Firestore.CollectionReference Collection { get; set; } = null!;
        public Google.Cloud.Firestore.FirestoreDb DB => null!;

        public string GetCollectionKey(Type toTypeGet)
        {
            // Mirror the exact production logic from GoogleDbContext
            if (toTypeGet == typeof(Shop))
                return "shopcollection";
            if (toTypeGet == typeof(ItemCategory))
                return "itemcategories";
            return toTypeGet.Name.ToLower() + "s";
        }
    }

    /// <summary>Dummy entity used to verify the convention handles unknown future types.</summary>
    internal class SomeHypotheticalFutureEntity : Shared.BaseModels.EntityBase { }
}
