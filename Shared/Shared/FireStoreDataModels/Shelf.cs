using Google.Cloud.Firestore;
using Shared.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class Shelf : EntityBase
    {
        [FirestoreProperty]
        public ICollection<ItemCategory> ItemCateogries { get; set; }
        [FirestoreProperty]
        public int SortIndex { get; set; } //for å konfigurere en butikk



    }
}
