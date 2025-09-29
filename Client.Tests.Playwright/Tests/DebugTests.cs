using Microsoft.Playwright;
using Xunit;

namespace Client.Tests.Playwright.Tests;

[Collection("Playwright")]
public class DebugTests
{
    private readonly PlaywrightFixture _fixture;
    private readonly string BaseUrl = "https://localhost:7072";

    public DebugTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DebugAdminPage_CaptureConsoleErrors()
    {
        var page = await _fixture.CreatePageAsync();
        var consoleMessages = new List<string>();
        var errors = new List<string>();

        try
        {
            // Capture console messages and errors
            page.Console += (_, e) =>
            {
                consoleMessages.Add($"[{e.Type}] {e.Text}");
                if (e.Type == "error")
                {
                    errors.Add(e.Text);
                }
            };

            page.PageError += (_, e) =>
            {
                errors.Add($"Page Error: {e}");
            };

            // Navigate to admin page
            await page.GotoAsync($"{BaseUrl}/admin");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(3000);

            // Get page content
            var content = await page.TextContentAsync("body");
            
            // Output debug information
            foreach (var msg in consoleMessages)
            {
                Console.WriteLine($"Console: {msg}");
            }
            
            foreach (var error in errors)
            {
                Console.WriteLine($"Error: {error}");
            }

            Console.WriteLine($"Page contains 'unhandled error': {content?.Contains("An unhandled error has occurred")}");
            Console.WriteLine($"Page content length: {content?.Length}");
            
            // This test is for debugging - always pass but show the info
            Assert.True(true, "Debug test completed");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DebugShoppingListPage_CaptureConsoleErrors()
    {
        var page = await _fixture.CreatePageAsync();
        var consoleMessages = new List<string>();
        var errors = new List<string>();

        try
        {
            // Capture console messages and errors
            page.Console += (_, e) =>
            {
                consoleMessages.Add($"[{e.Type}] {e.Text}");
                if (e.Type == "error")
                {
                    errors.Add(e.Text);
                }
            };

            page.PageError += (_, e) =>
            {
                errors.Add($"Page Error: {e}");
            };

            // Navigate to shopping list page with valid ID
            await page.GotoAsync($"{BaseUrl}/shoppinglist/test-list-1");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(3000);

            // Get page content
            var content = await page.TextContentAsync("body");
            
            // Output debug information
            foreach (var msg in consoleMessages)
            {
                Console.WriteLine($"Console: {msg}");
            }
            
            foreach (var error in errors)
            {
                Console.WriteLine($"Error: {error}");
            }

            Console.WriteLine($"Page contains 'unhandled error': {content?.Contains("An unhandled error has occurred")}");
            Console.WriteLine($"Page content length: {content?.Length}");
            
            // This test is for debugging - always pass but show the info
            Assert.True(true, "Debug test completed");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}