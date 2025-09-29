using Microsoft.Playwright;

namespace Client.Tests.Playwright.Tests
{
    [Collection("Playwright")]
    public class PageInspectionTests : IClassFixture<PlaywrightFixture>
    {
        private readonly PlaywrightFixture _fixture;
        private const string BaseUrl = "https://localhost:7072";

        public PageInspectionTests(PlaywrightFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task InspectHomePage()
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await page.GotoAsync(BaseUrl);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                var title = await page.TitleAsync();
                var content = await page.TextContentAsync("body");
                
                // Log what we actually see
                Console.WriteLine($"Title: {title}");
                Console.WriteLine($"Body content (first 500 chars): {content?[..Math.Min(500, content.Length)]}");
                
                // Check for navigation elements
                var navElements = await page.Locator("nav").CountAsync();
                var links = await page.Locator("a").CountAsync();
                
                Console.WriteLine($"Nav elements: {navElements}");
                Console.WriteLine($"Links found: {links}");
                
                Assert.True(true); // This test is just for inspection
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Fact]
        public async Task InspectOneShoppingListPage()
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                // Try to navigate to the shopping list page
                await page.GotoAsync($"{BaseUrl}/shopping/oneshoppinglistpage");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                var content = await page.TextContentAsync("body");
                Console.WriteLine($"OneShoppingListPage content (first 500 chars): {content?[..Math.Min(500, content.Length)]}");
                
                // Look for Syncfusion components
                var dropdowns = await page.Locator(".e-dropdownlist").CountAsync();
                var autocompletes = await page.Locator(".e-autocomplete").CountAsync();
                var inputs = await page.Locator("input.e-input").CountAsync();
                
                Console.WriteLine($"Syncfusion dropdowns: {dropdowns}");
                Console.WriteLine($"Syncfusion autocompletes: {autocompletes}");
                Console.WriteLine($"Syncfusion inputs: {inputs}");
                
                // Check if we need a shopping list ID
                var url = page.Url;
                Console.WriteLine($"Current URL: {url}");
                
                Assert.True(true); // This test is just for inspection
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/shopping/shoppinglistmainpage")]
        [InlineData("/shopping/managemyshopspage")]
        [InlineData("/admin")]
        public async Task InspectPageRoutes(string route)
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}{route}");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                var title = await page.TitleAsync();
                var content = await page.TextContentAsync("body");
                var url = page.Url;
                
                Console.WriteLine($"Route: {route}");
                Console.WriteLine($"Final URL: {url}");
                Console.WriteLine($"Title: {title}");
                Console.WriteLine($"Content length: {content?.Length}");
                Console.WriteLine($"Content preview: {content?[..Math.Min(200, content?.Length ?? 0)]}");
                Console.WriteLine("---");
                
                Assert.True(true); // This test is just for inspection
            }
            finally
            {
                await page.CloseAsync();
            }
        }
    }
}