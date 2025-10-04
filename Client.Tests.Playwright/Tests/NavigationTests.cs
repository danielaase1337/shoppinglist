using Microsoft.Playwright;

namespace Client.Tests.Playwright.Tests
{
    [Collection("Playwright")]
    public class NavigationTests : IClassFixture<PlaywrightFixture>
    {
        private readonly PlaywrightFixture _fixture;
        private const string BaseUrl = "https://localhost:7072";

        public NavigationTests(PlaywrightFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HomePage_ShouldLoadSuccessfully()
        {
            // Arrange
            var page = await _fixture.CreatePageAsync();
            
            try
            {
                // Act
                await page.GotoAsync(BaseUrl);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(2000); // Wait for Blazor to initialize
                
                // Assert
                var title = await page.TitleAsync();
                Assert.Contains("The Aase-broen's", title, StringComparison.OrdinalIgnoreCase);
                
                // Verify main navigation is present
                var navComponent = page.Locator("nav");
                await navComponent.WaitForAsync(new LocatorWaitForOptions 
                { 
                    State = WaitForSelectorState.Visible,
                    Timeout = 10000 
                });
                
                Assert.True(await navComponent.CountAsync() > 0, "Navigation should be present");
                
                // Should show shopping list content since Index page renders ShoppingListMainPage
                var content = await page.TextContentAsync("body");
                Assert.True(content?.Contains("Handlelister") == true, "Home page should show shopping lists");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Theory]
        [InlineData("/shoppinglist", "Handlelister")]
        [InlineData("/admin", "Admin")]
        public async Task NavigationPages_ShouldLoadCorrectly(string route, string expectedContent)
        {
            // Arrange
            var page = await _fixture.CreatePageAsync();
            
            try
            {
                // Act
                await page.GotoAsync($"{BaseUrl}{route}");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(3000); // Wait for full page load and API calls
                
                // Assert: Page should load without critical errors
                var pageText = await page.TextContentAsync("body");
                
                // Should not show unhandled error
                Assert.DoesNotContain("An unhandled error has occurred", pageText);
                
                // Should have substantial content
                Assert.True(!string.IsNullOrEmpty(pageText), $"Page {route} should have content");
                Assert.True(pageText.Length > 100, $"Page {route} should have substantial content, got: {pageText.Length} chars");
                
                // Should contain expected content
                Assert.Contains(expectedContent, pageText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Fact]
        public async Task ShoppingListMainPage_ShouldShowShoppingLists()
        {
            // Arrange
            var page = await _fixture.CreatePageAsync();
            
            try
            {
                // Act - Route is /shoppinglist based on @page directive
                await page.GotoAsync($"{BaseUrl}/shoppinglist");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(3000); // Wait for API calls
                
                // Assert: Page should display shopping lists or empty state
                var content = await page.TextContentAsync("body");
                
                // Should not show error
                Assert.DoesNotContain("An unhandled error has occurred", content);
                
                // Should have header
                Assert.Contains("Handlelister", content, StringComparison.OrdinalIgnoreCase);
                
                // Should show test data lists or empty message
                var hasLists = content.Contains("Ukeshandel", StringComparison.OrdinalIgnoreCase) ||
                              content.Contains("Middag i kveld", StringComparison.OrdinalIgnoreCase) ||
                              content.Contains("Det finnes ingen handlelister", StringComparison.OrdinalIgnoreCase);
                
                Assert.True(hasLists, "Page should show shopping lists or empty state message");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Fact]
        public async Task OneShoppingListPage_WithValidId_ShouldLoadCorrectly()
        {
            // Arrange
            var page = await _fixture.CreatePageAsync();
            
            try
            {
                // Act - Use test data ID from MemoryGenericRepository
                await page.GotoAsync($"{BaseUrl}/shoppinglist/test-list-1");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(4000); // Wait for API calls and Syncfusion initialization
                
                // Assert: Page should load the shopping list
                var content = await page.TextContentAsync("body");
                
                // Should not show error
                Assert.DoesNotContain("An unhandled error has occurred", content);
                
                // Should have substantial content
                Assert.True(!string.IsNullOrEmpty(content), "OneShoppingListPage should have content");
                Assert.True(content.Length > 300, "Page should have substantial content for shopping list");
                
                // Should show the test shopping list name "Ukeshandel"
                Assert.Contains("Ukeshandel", content, StringComparison.OrdinalIgnoreCase);
                
                // Should show items from test data
                var hasItems = content.Contains("Melk", StringComparison.OrdinalIgnoreCase) ||
                              content.Contains("Brød", StringComparison.OrdinalIgnoreCase) ||
                              content.Contains("Epler", StringComparison.OrdinalIgnoreCase);
                
                Assert.True(hasItems, "Page should show shopping list items from test data");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Fact]
        public async Task AdminPage_ShouldLoadDatabaseManagement()
        {
            // Arrange
            var page = await _fixture.CreatePageAsync();
            
            try
            {
                // Act
                await page.GotoAsync($"{BaseUrl}/admin");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(2000);
                
                // Assert: Page should load without authentication error
                var content = await page.TextContentAsync("body");
                
                // Should not show unhandled error
                Assert.DoesNotContain("An unhandled error has occurred", content);
                
                // Should show admin content
                Assert.Contains("Admin", content, StringComparison.OrdinalIgnoreCase);
                
                // Should have substantial content
                Assert.True(content?.Length > 200, "Admin page should have substantial content");
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        
        [Fact]
        public async Task ManageMyShopsPage_WithValidId_ShouldLoad()
        {
            // Arrange
            var page = await _fixture.CreatePageAsync();
            
            try
            {
                // Act - Use test shop ID from MemoryGenericRepository
                await page.GotoAsync($"{BaseUrl}/managemyshops/rema-1000");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(3000);
                
                // Assert
                var content = await page.TextContentAsync("body");
                
                // Should not show unhandled error
                Assert.DoesNotContain("An unhandled error has occurred", content);
                
                // Should have content
                Assert.True(!string.IsNullOrEmpty(content) && content.Length > 100, 
                    "ManageMyShopsPage should have content");
            }
            finally
            {
                await page.CloseAsync();
            }
        }
    }
}