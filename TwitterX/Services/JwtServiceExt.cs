using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TwitterX.Client.Services;

public static class JwtServicesExt
{
    public static IServiceCollection AddAuthenticationJwtBearer(this IServiceCollection services, JwtConfiguration jwtConfig)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig.Realm,
                ValidAudience = jwtConfig.Realm,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
            };
        });
        return services;
    }

    public sealed record JwtConfiguration
    {
        public string Realm { get; init; } = "jwtrealm";
        [Required] public required string Secret { get; init; }
        public int Duration { get; init; } = 15;
        public int RefreshDuration { get; init; } = 10; // TODO: Not used yet
        public string SigningAlgorithm { get; init; } = "HS256";
    }
}