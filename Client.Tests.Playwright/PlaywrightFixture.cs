using Microsoft.Playwright;

namespace Client.Tests.Playwright
{
    public class PlaywrightFixture : IAsyncLifetime
    {
        private readonly List<IBrowserContext> _contexts = new();

        public IPlaywright Playwright { get; private set; } = null!;
        public IBrowser Browser { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            // Install browsers if not already installed
            Program.Main(new[] { "install" });

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                SlowMo = 0
            });
        }

        public async Task<IPage> CreatePageAsync()
        {
            var contextOptions = new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                BaseURL = "https://localhost:7072"
            };

            var context = await Browser.NewContextAsync(contextOptions);
            _contexts.Add(context);

            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(20000);
            page.SetDefaultNavigationTimeout(20000);

            return page;
        }

        public async Task DisposeAsync()
        {
            foreach (var context in _contexts)
            {
                if (context != null)
                {
                    await context.CloseAsync();
                }
            }

            if (Browser is not null)
            {
                await Browser.CloseAsync();
            }

            Playwright?.Dispose();
        }
    }
}