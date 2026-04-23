namespace Shared
{
    /// <summary>
    /// Controls whether the system tracks stock levels for a ShopItem.
    /// Track = include in inventory/stock checks; DoNotTrack = purely shopping-list use.
    /// </summary>
    public enum StockBehaviour { Track, DoNotTrack }
}
