namespace BlazorApp.Client.Resources;

/// <summary>
/// Marker class for IStringLocalizer&lt;SharedResources&gt; DI registration.
/// Resource files live alongside this class:
///   SharedResources.nb-NO.resx  — Norwegian (UI default for v1)
///   SharedResources.en.resx     — English (empty template; activate via culture config)
///
/// Usage in Razor pages:
///   @inject IStringLocalizer&lt;SharedResources&gt; L
///   &lt;h2&gt;@L["ShoppingLists_PageTitle"]&lt;/h2&gt;
///
/// Key naming convention: {PageOrScope}_{DescriptiveName}
///   - Page-specific:  ShoppingLists_PageTitle, OneShoppingList_SortLabel
///   - Common/shared:  Common_Save, Common_Cancel, Common_Loading
/// </summary>
public class SharedResources { }
