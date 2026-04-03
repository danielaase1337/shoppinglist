using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BlazorApp.Client.Auth;

public class SwaAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public SwaAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var authData = await _httpClient.GetFromJsonAsync<SwaAuthData>("/.auth/me", cts.Token);

            if (authData?.ClientPrincipal == null ||
                authData.ClientPrincipal.UserDetails == null)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var principal = authData.ClientPrincipal;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, principal.UserDetails),
                new Claim(ClaimTypes.NameIdentifier, principal.UserId ?? principal.UserDetails),
            };

            foreach (var role in principal.UserRoles ?? [])
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, principal.IdentityProvider ?? "aad");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}

public class SwaAuthData
{
    public SwaClientPrincipal? ClientPrincipal { get; set; }
}

public class SwaClientPrincipal
{
    public string? IdentityProvider { get; set; }
    public string? UserId { get; set; }
    public string? UserDetails { get; set; }
    public IEnumerable<string>? UserRoles { get; set; }
}
