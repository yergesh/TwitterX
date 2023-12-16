using System.Security.Claims;
using TwitterX.Client.Services;

namespace TwitterX.Client.Middleware;

public sealed class RefreshTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IJwtService _jwtService;

    public RefreshTokenMiddleware(RequestDelegate next, IJwtService jwtService)
    {
        _next = next;
        _jwtService = jwtService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity is ClaimsIdentity identity && identity.IsAuthenticated)
        {
            var twitterXUser = TwitterXClaimUser.FromClaimsPrincipal(context.User);
            if (twitterXUser != null)
            {
                context.SetTwitterXClaimUser(twitterXUser);
            }
        }

        await _next(context); // Call the next middleware in the pipeline

        if (context.User.Identity is ClaimsIdentity identity1 && identity1.IsAuthenticated)
        {
            if (context.GetTwitterXClaimUser() is { } twitterXUser)
            {
                // Generate new token
                (string newToken, string expire) = _jwtService.GenerateToken(twitterXUser.Country, twitterXUser.Mobile);

                if (!context.Response.HasStarted) // Check if the response has not yet started
                {
                    // Add new token to response headers
                    context.Response.Headers.Add("New-Token", newToken);
                    context.Response.Headers.Add("New-Token-Expire", expire);
                }

            }
        }
    }
}

public static class HttpContextExtensions
{
    private const string TWITTER_CLAIM_USER = "TwitterXClaimUser";

    public static void SetTwitterXClaimUser(this HttpContext context, TwitterXClaimUser twitterXClaimUser)
        => context.Items[TWITTER_CLAIM_USER] = twitterXClaimUser;

    public static TwitterXClaimUser? GetTwitterXClaimUser(this HttpContext context)
        => context.Items[TWITTER_CLAIM_USER] is TwitterXClaimUser { } twitterXUser ? twitterXUser : null;
}