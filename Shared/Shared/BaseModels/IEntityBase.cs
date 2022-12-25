namespace Shared.BaseModels
{
    internal interface IEntityBase
    {
        string CssComleteEditClassName { get; }
        bool EditClicked { get; set; }
        string Id { get; set; }
        string Name { get; set; }
    }
}