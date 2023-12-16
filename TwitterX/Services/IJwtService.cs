
namespace TwitterX.Client.Services
{
    public interface IJwtService
    {
        (string, string) GenerateToken(string country, string mobile);
    }
}