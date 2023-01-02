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
    public class ShopItem : EntityBase
    {
        [FirestoreProperty]
        public string Unit { get; set; } //Stk, vekt osv.. 
        [FirestoreProperty]
        public ItemCategory ItemCategory { get; set; }

    }
}
