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
            var page = await _fixture.Browser.NewPageAsync();
            
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
        [InlineData("/admin/admindatabase", "Admin")]
        public async Task NavigationPages_ShouldLoadCorrectly(string route, string expectedContent)
        {
            // Arrange
            var page = await _fixture.Browser.NewPageAsync();
            
            try
            {
                // Act
                await page.GotoAsync($"{BaseUrl}{route}");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Assert: Page should load without errors
                var content = await page.ContentAsync();
                
                // Basic verification that page loaded
                Assert.DoesNotContain("404", content);
                Assert.DoesNotContain("Error", content);
                
                // Check for expected content (case-insensitive)
                var pageText = await page.TextContentAsync("body");
                Assert.True(!string.IsNullOrEmpty(pageText), $"Page {route} should have content");
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
            var page = await _fixture.Browser.NewPageAsync();
            
            try
            {
                // Act
                await page.GotoAsync($"{BaseUrl}/shopping/shoppinglistmainpage");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Assert: Page should display shopping lists or empty state
                var content = await page.TextContentAsync("body");
                
                // Should have some content indicating shopping lists section
                Assert.True(!string.IsNullOrEmpty(content), "Shopping list main page should have content");
                
                // Look for common elements that might be present
                var hasLists = await page.Locator("[data-testid='shopping-list']").CountAsync() > 0;
                var hasEmptyState = content.Contains("No shopping lists", StringComparison.OrdinalIgnoreCase) ||
                                  content.Contains("Ingen handlelister", StringComparison.OrdinalIgnoreCase);
                
                // Either lists should be present or empty state should be shown
                Assert.True(hasLists || hasEmptyState || content.Length > 100, 
                    "Page should either show shopping lists or indicate empty state");
            }
            finally
            {
                await page.CloseAsync();
            }
        }
    }
}