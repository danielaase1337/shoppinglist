namespace Shared.FireStoreDataModels
{
    public enum MealCategory
    {
        KidsLike,    // Barn-favoritter
        Fish,        // Fisk
        Meat,        // Kjøtt
        Vegetarian,  // Vegetar
        Chicken,     // Kylling
        Pasta,       // Pasta
        Celebration, // Fest
        Other        // Annet
    }

    public enum MealType
    {
        FreshCook,   // Lager fra bunnen
        Frozen,      // Frossenmat
        PrePrepped,  // Ferdigpreppert
        Takeout      // Bestilt mat
    }

    public enum MealEffort
    {
        Quick,   // ≤20 min — frossen, ferdig, takeout
        Normal,  // 20–45 min — vanlig hverdag
        Weekend  // 45+ min — lasagne, langkokt, festmat
    }
}
