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
                await page.WaitForTimeoutAsync(2000);
                
                var title = await page.TitleAsync();
                var content = await page.TextContentAsync("body");
                
                // Log what we actually see
                Console.WriteLine($"Title: {title}");
                if (content != null)
                {
                    Console.WriteLine($"Body content (first 500 chars): {content[..Math.Min(500, content.Length)]}");
                }
                
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
                // Correct route with test data ID
                await page.GotoAsync($"{BaseUrl}/shoppinglist/test-list-1");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(4000); // Wait for API calls and Syncfusion
                
                var content = await page.TextContentAsync("body");
                if (content != null)
                {
                    Console.WriteLine($"OneShoppingListPage content (first 800 chars): {content[..Math.Min(800, content.Length)]}");
                }
                
                // Look for Syncfusion components
                var dropdowns = await page.Locator(".e-dropdownlist").CountAsync();
                var autocompletes = await page.Locator(".e-autocomplete").CountAsync();
                var inputs = await page.Locator("input.e-input").CountAsync();
                var shopDropdowns = await page.Locator(".e-dropdownlist").AllAsync();
                
                Console.WriteLine($"Syncfusion dropdowns: {dropdowns}");
                Console.WriteLine($"Syncfusion autocompletes: {autocompletes}");
                Console.WriteLine($"Syncfusion inputs: {inputs}");
                Console.WriteLine($"Total dropdown count: {shopDropdowns.Count}");
                
                // Check URL and content
                var url = page.Url;
                Console.WriteLine($"Current URL: {url}");
                Console.WriteLine($"Content contains 'Ukeshandel': {content?.Contains("Ukeshandel")}");
                Console.WriteLine($"Content contains 'Melk': {content?.Contains("Melk")}");
                
                Assert.True(true); // This test is just for inspection
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/shoppinglist")]
        [InlineData("/admin")]
        [InlineData("/shoppinglist/test-list-1")]
        [InlineData("/managemyshops/rema-1000")]
        public async Task InspectPageRoutes(string route)
        {
            var page = await _fixture.CreatePageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}{route}");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(3000);
                
                var title = await page.TitleAsync();
                var content = await page.TextContentAsync("body");
                var url = page.Url;
                
                // Check for errors
                var hasError = content?.Contains("An unhandled error has occurred") ?? false;
                var hasLoading = content?.Contains("Laster app...") ?? false;
                
                Console.WriteLine($"Route: {route}");
                Console.WriteLine($"Final URL: {url}");
                Console.WriteLine($"Title: {title}");
                Console.WriteLine($"Content length: {content?.Length ?? 0}");
                Console.WriteLine($"Has error: {hasError}");
                Console.WriteLine($"Still loading: {hasLoading}");
                if (content != null)
                {
                    Console.WriteLine($"Content preview: {content[..Math.Min(300, content.Length)]}");
                }
                Console.WriteLine("---");
                
                Assert.True(true); // This test is just for inspection
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        
        [Fact]
        public async Task InspectAPIConnection()
        {
            var page = await _fixture.CreatePageAsync();
            var apiCalls = new List<string>();
            var failedCalls = new List<string>();
            
            try
            {
                // Capture network requests
                page.Request += (_, request) =>
                {
                    if (request.Url.Contains("/api/"))
                    {
                        apiCalls.Add($"{request.Method} {request.Url}");
                    }
                };
                
                page.RequestFailed += (_, request) =>
                {
                    if (request.Url.Contains("/api/"))
                    {
                        failedCalls.Add($"FAILED: {request.Method} {request.Url} - {request.Failure}");
                    }
                };
                
                await page.GotoAsync($"{BaseUrl}/shoppinglist");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(3000);
                
                Console.WriteLine("=== API Calls ===");
                foreach (var call in apiCalls)
                {
                    Console.WriteLine(call);
                }
                
                Console.WriteLine("\n=== Failed API Calls ===");
                foreach (var fail in failedCalls)
                {
                    Console.WriteLine(fail);
                }
                
                Console.WriteLine($"\nTotal API calls: {apiCalls.Count}");
                Console.WriteLine($"Failed API calls: {failedCalls.Count}");
                
                Assert.True(true); // This test is just for inspection
            }
            finally
            {
                await page.CloseAsync();
            }
        }
    }
}