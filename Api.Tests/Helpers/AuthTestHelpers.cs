using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Api.Tests.Helpers;

public static class AuthTestHelpers
{
    public static HttpRequest CreateAuthenticatedRequest(
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

        var context = new DefaultHttpContext();
        context.Request.Headers["x-ms-client-principal"] = encoded;
        return context.Request;
    }

    public static HttpRequest CreateUnauthenticatedRequest()
    {
        var context = new DefaultHttpContext();
        return context.Request;
    }

    public static HttpRequest CreateRequestWithRoles(params string[] roles)
        => CreateAuthenticatedRequest(roles: roles);
}
