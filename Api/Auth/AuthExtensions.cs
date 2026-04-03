using Microsoft.Azure.Functions.Worker.Http;

namespace Api.Auth;

/// <summary>
/// HttpRequestData extension helpers for SWA x-ms-client-principal auth.
/// Use these in controllers to access the current user without repeating parse logic.
/// </summary>
public static class AuthExtensions
{
    public static ClientPrincipal? GetClientPrincipal(this HttpRequestData request)
        => ClientPrincipal.Parse(request);

    public static bool IsAuthenticated(this HttpRequestData request)
        => request.GetClientPrincipal()?.IsAuthenticated == true;

    public static string? GetUserId(this HttpRequestData request)
        => request.GetClientPrincipal()?.UserId;

    public static string? GetUserName(this HttpRequestData request)
        => request.GetClientPrincipal()?.UserDetails;
}
