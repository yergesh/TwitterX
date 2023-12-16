using System.Security.Claims;

namespace TwitterX.Client.Middleware;

public sealed class TwitterXClaimUser
{
    public required string Country { get; init; }
    public required string Mobile { get; init; }

    public static TwitterXClaimUser? FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated is true
            && principal.FindFirstValue("c") is { } country
            && principal.FindFirstValue("m") is { } mobile)
        {
            return new TwitterXClaimUser
            {

                Country = country,
                Mobile = mobile,
            };
        }
        else
        {
            return null;
        }
    }
}
