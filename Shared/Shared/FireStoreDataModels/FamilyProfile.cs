using Google.Cloud.Firestore;
using Shared.BaseModels;
using System.Collections.Generic;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class FamilyMember
    {
        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public AgeGroup AgeGroup { get; set; }

        [FirestoreProperty]
        public string? DietaryNotes { get; set; }
    }

    [FirestoreData]
    public class FamilyProfile : EntityBase
    {
        [FirestoreProperty]
        public ICollection<FamilyMember> Members { get; set; }

        public FamilyProfile()
        {
            Members = new List<FamilyMember>();
        }
    }
}
