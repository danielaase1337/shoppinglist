using Shared.BaseModels;
using System.Collections.Generic;
using System.Linq;

namespace Shared.HandlelisteModels
{
    public class FamilyMemberModel
    {
        public string Name { get; set; }
        public AgeGroup AgeGroup { get; set; }
        public string? DietaryNotes { get; set; }
    }

    public class FamilyProfileModel : EntityBase
    {
        public ICollection<FamilyMemberModel> Members { get; set; }

        public FamilyProfileModel()
        {
            Members = new List<FamilyMemberModel>();
        }

        public override bool IsValid() => Members != null && Members.Any();
    }
}
