using System.Text;
using System.Text.Json;

namespace Api.Tests.Helpers;

/// <summary>
/// Builds Azure Functions HttpRequestData instances with x-ms-client-principal headers
/// for testing SWA auth scenarios without a live SWA environment.
/// </summary>
public static class AuthTestHelpers
{
    public static TestHttpRequestData CreateAuthenticatedRequest(
        string userId = "test-user-123",
        string userDetails = "testuser@example.com",
        string identityProvider = "aad",
        string[]? roles = null)
    {
        var principal = new
        {
            identityProvider = identityProvider,
            userId = userId,
            userDetails = userDetails,
            userRoles = roles ?? new[] { "anonymous", "authenticated" }
        };

        var json = JsonSerializer.Serialize(principal);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        var req = TestHttpFactory.CreateGetRequest();
        req.Headers.Add("x-ms-client-principal", encoded);
        return req;
    }

    public static TestHttpRequestData CreateUnauthenticatedRequest()
        => TestHttpFactory.CreateGetRequest();

    public static TestHttpRequestData CreateRequestWithRoles(params string[] roles)
        => CreateAuthenticatedRequest(roles: roles);
}
