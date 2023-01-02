using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class ShelfCategory
    {
        public ShelfCategory()
        {

        }
        public ShelfCategory(string name, int id)
        {
            Name = name;
            Id = id;
        }
        [FirestoreProperty]
        public string Name { get; set; }
        [FirestoreProperty]
        public int Id { get; set; }
        [FirestoreProperty]
        public string Description { get; set; }

        public static List<ShelfCategory> GetDefaults()
        {
            var res = new List<ShelfCategory>()
            {
                new ShelfCategory("Brus", 1),
                new ShelfCategory("Meieri", 2),
                new ShelfCategory("FruktOgGrønt", 3),
                new ShelfCategory("Brød", 4),
                new ShelfCategory("Kaffe", 5),
                new ShelfCategory("JuiceOgFrokost", 6),
                new ShelfCategory("Kjøtt", 7),
                new ShelfCategory("Fisk", 8),
                new ShelfCategory("KjølevarerMiddag", 9),
                new ShelfCategory("KjølevarerPålegg", 10),
                new ShelfCategory("Oljer", 11),
                new ShelfCategory("Krydder", 12),
                new ShelfCategory("Frysevarer", 13),
                new ShelfCategory("Hermetikk", 15),
                new ShelfCategory("Pasta", 16),
                new ShelfCategory("Asiatisk", 17),
                new ShelfCategory("Alergivarer", 18),
                new ShelfCategory("Dyremat", 19),
                new ShelfCategory("Toalletpapir", 20),
                new ShelfCategory("Vaskemidler", 21),
                new ShelfCategory("Knekkebrød", 22),
                new ShelfCategory("Bakevarer", 23),
                new ShelfCategory("Chips", 24),
                new ShelfCategory("Øl", 25),
                new ShelfCategory("Brus", 26),
                new ShelfCategory("Snop", 27),
                new ShelfCategory("Is", 28),
                new ShelfCategory("Blader", 29),
                new ShelfCategory("Kasse", 30),
            };
            return res;
        }
    }
}
