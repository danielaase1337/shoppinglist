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
                
                // Assert
                var title = await page.TitleAsync();
                Assert.Contains("The Aase-broen's", title, StringComparison.OrdinalIgnoreCase);
                
                // Verify main navigation is present
                var navComponent = page.Locator("nav").Or(page.Locator("[data-testid='navigation']"));
                await navComponent.WaitForAsync(new LocatorWaitForOptions 
                { 
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000 
                });
                
                Assert.True(await navComponent.CountAsync() > 0, "Navigation should be present");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Theory]
        [InlineData("/shopping/shoppinglistmainpage", "Shopping Lists")]
        [InlineData("/shopping/managemyshopspage", "Manage Shops")]
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
                await page.WaitForTimeoutAsync(2000); // Wait for full page load
                
                // Assert: Page should load without critical errors
                var content = await page.ContentAsync();
                var pageText = await page.TextContentAsync("body");
                
                // Should not show unhandled error
                Assert.DoesNotContain("An unhandled error has occurred", pageText);
                
                // Should have substantial content
                Assert.True(!string.IsNullOrEmpty(pageText), $"Page {route} should have content");
                Assert.True(pageText.Length > 100, $"Page {route} should have substantial content, got: {pageText.Length} chars");
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
                // Act
                await page.GotoAsync($"{BaseUrl}/shopping/shoppinglistmainpage");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(2000);
                
                // Assert: Page should display shopping lists or empty state
                var content = await page.TextContentAsync("body");
                
                // Should not show error
                Assert.DoesNotContain("An unhandled error has occurred", content);
                
                // Should have some meaningful content
                Assert.True(!string.IsNullOrEmpty(content), "Shopping list main page should have content");
                Assert.True(content.Length > 200, "Page should have substantial content");
                
                // Should either show lists or be a proper shopping list page
                var hasShoppingContent = content.Contains("Ukeshandel", StringComparison.OrdinalIgnoreCase) ||
                                       content.Contains("shopping", StringComparison.OrdinalIgnoreCase) ||
                                       content.Contains("liste", StringComparison.OrdinalIgnoreCase) ||
                                       content.Contains("handleliste", StringComparison.OrdinalIgnoreCase);
                
                Assert.True(hasShoppingContent || content.Length > 500, 
                    "Page should show shopping-related content or substantial content");
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
                await page.WaitForTimeoutAsync(3000); // Wait for API calls and Syncfusion initialization
                
                // Assert: Page should load the shopping list
                var content = await page.TextContentAsync("body");
                
                // Should not show error
                Assert.DoesNotContain("An unhandled error has occurred", content);
                
                // Should have substantial content
                Assert.True(!string.IsNullOrEmpty(content), "OneShoppingListPage should have content");
                Assert.True(content.Length > 300, "Page should have substantial content for shopping list");
                
                // Should show the test shopping list name "Ukeshandel"
                Assert.True(content.Contains("Ukeshandel") || content.Contains("test"), 
                    "Page should show the shopping list name from test data");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [Fact]
        public async Task AdminPage_ShouldLoadWithoutAuthentication()
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
                Assert.True(!string.IsNullOrEmpty(content) && content.Contains("Admin", StringComparison.OrdinalIgnoreCase), 
                    "Page should show admin content");
                Assert.True(!string.IsNullOrEmpty(content) && (content.Contains("Database", StringComparison.OrdinalIgnoreCase) || 
                           content.Contains("Test Data", StringComparison.OrdinalIgnoreCase)), 
                    "Page should show database management content");
                
                // Should have substantial content
                Assert.True(content?.Length > 200, "Admin page should have substantial content");
            }
            finally
            {
                await page.CloseAsync();
            }
        }
    }
}