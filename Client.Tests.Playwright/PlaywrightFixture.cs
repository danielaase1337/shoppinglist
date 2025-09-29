using Microsoft.Playwright;

namespace Client.Tests.Playwright
{
    public class PlaywrightFixture : IAsyncLifetime
    {
        public IPlaywright Playwright { get; private set; } = null!;
        public IBrowser Browser { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            // Install browsers if not already installed
            Program.Main(new[] { "install" });

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true, // Set to false for debugging
                SlowMo = 100 // Slow down operations for better visibility during debugging
            });
        }

        public async Task DisposeAsync()
        {
            await Browser?.CloseAsync();
            Playwright?.Dispose();
        }
    }
}