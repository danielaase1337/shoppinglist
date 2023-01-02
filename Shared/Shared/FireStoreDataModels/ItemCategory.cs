
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
    public class ItemCategory:EntityBase

    {
        public ItemCategory()
        {

        }
        public ItemCategory(string name, string id)
        {
            Name = name;
            Id = id; 
        }

    }

}
