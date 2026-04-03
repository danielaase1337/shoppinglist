using System.Text.Json;
using Microsoft.Playwright;

namespace Client.Tests.Playwright.Tests;

/// <summary>
/// E2E tests for SWA authentication flow.
///
/// Local dev note: SWA auth infrastructure (/.auth/me, /.auth/login/aad) is only available
/// when running behind Azure Static Web Apps. Locally, the app calls /.auth/me but gets a
/// 404/empty response, so the user appears unauthenticated.
///
/// Tests that require live SWA infrastructure are marked [Trait("Category", "RequiresSWA")]
/// and should be skipped in local CI.
///
/// Tests that verify UI behaviour (login link visible, href correct) can run locally
/// once Blair's LoginDisplay component is in place.
/// </summary>
[Collection("Playwright")]
public class AuthenticationTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;
    private const string BaseUrl = "https://localhost:7072";

    // Minimal SWA /.auth/me response shapes
    private static readonly string UnauthenticatedMeResponse =
        JsonSerializer.Serialize(new { clientPrincipal = (object?)null });

    private static readonly string AuthenticatedMeResponse =
        JsonSerializer.Serialize(new
        {
            clientPrincipal = new
            {
                identityProvider = "aad",
                userId = "e2e-test-user-001",
                userDetails = "testuser@example.com",
                userRoles = new[] { "anonymous", "authenticated" }
            }
        });

    public AuthenticationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that a "Logg inn" link is rendered in the nav when /.auth/me returns null.
    /// Depends on Blair's LoginDisplay component being in place.
    /// </summary>
    [Fact]
    public async Task LoginLink_IsVisible_WhenNotAuthenticated()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.RouteAsync("**/.auth/me", async route =>
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "application/json",
                    Body = UnauthenticatedMeResponse
                }));

            await page.GotoAsync(BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);

            var loginLink = page.Locator("a[href*='/.auth/login']")
                .Or(page.Locator("a:text('Logg inn')")
                .Or(page.Locator("[data-testid='login-link']")));

            var count = await loginLink.CountAsync();
            Assert.True(count > 0,
                "A 'Logg inn' link must be visible when the user is unauthenticated. " +
                "Blair's LoginDisplay component should render this in the nav bar.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies the login link points to the Microsoft SWA auth endpoint (D14: Microsoft only).
    /// </summary>
    [Fact]
    public async Task LoginLink_PointsToCorrectSwaEndpoint()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.RouteAsync("**/.auth/me", async route =>
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "application/json",
                    Body = UnauthenticatedMeResponse
                }));

            await page.GotoAsync(BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);

            // Find any login-related anchor element
            var loginLink = page.Locator("a[href*='/.auth/login/aad']")
                .Or(page.Locator("a[href*='/.auth/login']"));

            var count = await loginLink.CountAsync();
            Assert.True(count > 0,
                "Login link must exist. Once found, verify it targets the Microsoft AAD provider.");

            if (count > 0)
            {
                var href = await loginLink.First.GetAttributeAsync("href");
                Assert.True(href?.Contains("/.auth/login/aad") == true,
                    "Login link must target /.auth/login/aad (Microsoft provider — D14). " +
                    "GitHub provider was removed per decision D14.");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies a logout link or user display appears when /.auth/me returns an authenticated user.
    /// Depends on Blair's SwaAuthenticationStateProvider and LoginDisplay being in place.
    /// </summary>
    [Fact]
    public async Task LogoutLink_IsVisible_WhenAuthenticated()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Mock /.auth/me to simulate a logged-in Microsoft user
            await page.RouteAsync("**/.auth/me", async route =>
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "application/json",
                    Body = AuthenticatedMeResponse
                }));

            await page.GotoAsync(BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);

            var logoutLink = page.Locator("a[href*='/.auth/logout']")
                .Or(page.Locator("a:text('Logg ut')")
                .Or(page.Locator("[data-testid='logout-link']")));

            var count = await logoutLink.CountAsync();
            Assert.True(count > 0,
                "A logout link (or user name + logout) must be visible when /.auth/me returns " +
                "an authenticated principal. Blair's LoginDisplay component should render this.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies that a protected route shows a not-authorized message or redirects
    /// when the user is unauthenticated. SWA-level 302 redirect only fires in the cloud;
    /// locally Blazor's AuthorizeRouteView shows the NotAuthorized fragment.
    ///
    /// Marked RequiresSWA because the actual 302 HTTP redirect only works behind
    /// the Azure Static Web Apps gateway.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresSWA")]
    public async Task ProtectedRoute_RedirectsToLogin_WhenUnauthenticated()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Do NOT mock /.auth/me so the app sees an unauthenticated user

            var response = await page.GotoAsync($"{BaseUrl}/shoppinglist");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);

            // In SWA, the gateway returns 302 → /.auth/login/aad for protected routes.
            // Locally, Blazor's AuthorizeRouteView shows a not-authorized fragment.
            var finalUrl = page.Url;
            var pageText = await page.TextContentAsync("body");

            var isRedirectedToLogin = finalUrl.Contains("/.auth/login");
            var showsNotAuthorizedMessage =
                pageText.Contains("Logg inn", StringComparison.OrdinalIgnoreCase) ||
                pageText.Contains("ikke tilgang", StringComparison.OrdinalIgnoreCase) ||
                pageText.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

            Assert.True(isRedirectedToLogin || showsNotAuthorizedMessage,
                $"Unauthenticated access to /shoppinglist should either redirect to login or show " +
                $"a not-authorized message. Actual URL: {finalUrl}");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
