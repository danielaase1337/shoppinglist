using Shared.BaseModels;

namespace Shared.HandlelisteModels
{
    public class ShoppingListBaseModel: EntityBase
    {
        public bool IsDone { get; set; } = false;

        public new string CssComleteEditClassName
        {
            get
            {
                if (IsDone)
                    return "completed";
                else if (EditClicked)
                    return "edit";
                else return "";
            }
        }

    }
}
