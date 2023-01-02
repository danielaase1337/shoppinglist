using Google.Cloud.Firestore;

namespace Shared.BaseModels
{
    
    public class EntityBase : IEntityBase
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

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
