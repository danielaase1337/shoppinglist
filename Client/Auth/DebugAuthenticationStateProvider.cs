using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorApp.Client.Auth;

/// <summary>
/// Used in DEBUG builds only. Returns a pre-authenticated "Debug User" so the
/// SWA /.auth/me endpoint is never called and auth is always bypassed locally.
/// </summary>
public class DebugAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState _debugState = BuildDebugState();

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(_debugState);

    private static AuthenticationState BuildDebugState()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Debug User"),
            new Claim(ClaimTypes.NameIdentifier, "debug-user-id"),
            new Claim(ClaimTypes.Role, "authenticated"),
        };

        var identity = new ClaimsIdentity(claims, "debug");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
