using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace Api.Auth;

/// <summary>
/// Represents the user principal injected by Azure Static Web Apps via the
/// x-ms-client-principal header (base64-encoded JSON). Microsoft provider only (D14).
/// </summary>
public class ClientPrincipal
{
    public string? IdentityProvider { get; set; }
    public string? UserId { get; set; }
    public string? UserDetails { get; set; }
    public IEnumerable<string>? UserRoles { get; set; }

    /// <summary>
    /// Parses the x-ms-client-principal header from the incoming request.
    /// Returns null if the header is absent or malformed — never throws.
    /// </summary>
    public static ClientPrincipal? Parse(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("x-ms-client-principal", out var values))
            return null;

        var encoded = values?.FirstOrDefault();
        if (string.IsNullOrEmpty(encoded))
            return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            return JsonSerializer.Deserialize<ClientPrincipal>(decoded, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            // Malformed header — treat as unauthenticated, never leak parse errors
            return null;
        }
    }

    /// <summary>
    /// True when SWA has injected a valid authenticated principal with the "authenticated" role.
    /// </summary>
    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(UserId) &&
        UserRoles?.Contains("authenticated") == true;
}
