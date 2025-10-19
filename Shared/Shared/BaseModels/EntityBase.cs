using Google.Cloud.Firestore;
using System;

namespace Shared.BaseModels
{
    
    public class EntityBase : IEntityBase
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public DateTime? LastModified { get; set; }

        public string CssComleteEditClassName
        {
            get
            {
                if (EditClicked)
                    return "edit";
                else return "";
            }
        }
        public bool EditClicked { get; set; }
        public virtual bool IsValid()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}
