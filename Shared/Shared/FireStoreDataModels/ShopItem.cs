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

        // #73 — Staple/basic item (e.g. milk, bread — always stocked)
        [FirestoreProperty]
        public bool IsBasic { get; set; }

        // #75 — Controls inventory tracking behaviour for this item
        [FirestoreProperty]
        public StockBehaviour StockBehaviour { get; set; } = StockBehaviour.Track;

        // #76 — Standard purchase unit size (e.g. carrots come in 1 kg bags)
        [FirestoreProperty]
        public double StandardPurchaseQuantity { get; set; } // 0 = not set

        [FirestoreProperty]
        public string StandardPurchaseUnit { get; set; } // e.g. "kg", "stk", "l" — null/empty = not set
    }
}
