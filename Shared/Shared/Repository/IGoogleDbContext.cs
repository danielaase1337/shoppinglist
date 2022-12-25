using Google.Cloud.Firestore;

namespace Shared.Repository
{
    public interface IGoogleDbContext
    {
        string CollectionKey { get; set; }
        CollectionReference Collection { get; set; }
        FirestoreDb DB { get; }
        //string GetCollectionKey(object toTypeGet);
        string GetCollectionKey(Type toTypeGet);
    }
}