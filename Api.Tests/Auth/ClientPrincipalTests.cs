using Api.Auth;
using Api.Tests.Helpers;
using Xunit;

namespace Api.Tests.Auth;

public class ClientPrincipalTests
{
    [Fact]
    public void Parse_WithValidHeader_ReturnsPrincipal()
    {
        var request = AuthTestHelpers.CreateAuthenticatedRequest(
            userId: "abc123",
            userDetails: "daniel@example.com");

        var principal = ClientPrincipal.Parse(request);

        Assert.NotNull(principal);
        Assert.Equal("abc123", principal.UserId);
        Assert.Equal("daniel@example.com", principal.UserDetails);
    }

    [Fact]
    public void Parse_WithNoHeader_ReturnsNull()
    {
        var request = AuthTestHelpers.CreateUnauthenticatedRequest();
        var principal = ClientPrincipal.Parse(request);
        Assert.Null(principal);
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedRole_ReturnsTrue()
    {
        var request = AuthTestHelpers.CreateAuthenticatedRequest(
            roles: new[] { "anonymous", "authenticated" });

        var principal = ClientPrincipal.Parse(request);
        Assert.True(principal?.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WithOnlyAnonymousRole_ReturnsFalse()
    {
        var request = AuthTestHelpers.CreateAuthenticatedRequest(
            roles: new[] { "anonymous" });

        var principal = ClientPrincipal.Parse(request);
        Assert.False(principal?.IsAuthenticated);
    }

    [Fact]
    public void Parse_WithMalformedHeader_ReturnsNull()
    {
        var req = TestHttpFactory.CreateGetRequest();
        req.Headers.Add("x-ms-client-principal", "not-valid-base64!!!");

        var principal = ClientPrincipal.Parse(req);
        Assert.Null(principal);
    }

    [Fact]
    public void IsAuthenticated_Extension_WithAuthenticatedRequest_ReturnsTrue()
    {
        var request = AuthTestHelpers.CreateAuthenticatedRequest();
        Assert.True(request.IsAuthenticated());
    }

    [Fact]
    public void GetUserId_Extension_ReturnsCorrectId()
    {
        var request = AuthTestHelpers.CreateAuthenticatedRequest(userId: "xyz789");
        Assert.Equal("xyz789", request.GetUserId());
    }
}
