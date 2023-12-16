using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static TwitterX.Client.Services.JwtServicesExt;

namespace TwitterX.Client.Services;

public sealed class JwtService : IJwtService
{
    private readonly JwtConfiguration _jwtConfig;

    public JwtService(IOptions<JwtConfiguration> jwtConfig)
    {
        _jwtConfig = jwtConfig?.Value ?? throw new ArgumentNullException(nameof(jwtConfig));
    }

    public (string, string) GenerateToken(string country, string mobile)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtConfig.Realm,
            Audience = _jwtConfig.Realm,
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim("c", country ?? ""),
                new Claim("m", mobile ?? ""),
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.Duration),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), tokenDescriptor.Expires.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
