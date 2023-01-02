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
    public class ShoppingList: EntityBase
    {
        [FirestoreProperty]
        public string ListId { get; set; }
        
        [FirestoreProperty] 
        public ICollection<ShoppingListItem> ShoppingItems { get; set; }
        
        [FirestoreProperty] 
        public bool IsDone { get; set; }
    }
}
