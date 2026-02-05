using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace tyss
{
    public interface ITokenGenerator
    {
        void LogTokens();
    }

    public class DbgTokenGenerator : ITokenGenerator
    {
        private readonly IConfiguration _configuration;

        public DbgTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void LogTokens()
        {
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var key = _configuration["Jwt:Key"];

            var adminToken = GenerateToken(key, issuer, audience, false);
            var guestToken = GenerateToken(key, issuer, audience, true);

            Console.WriteLine($"\n[JWT ADMIN TOKEN]:\n{adminToken}\n");
            Console.WriteLine($"\n[JWT GUEST TOKEN]:\n{guestToken}\n");
        }

        private static string GenerateToken(string secretKey, string issuer, string audience, bool isGuest)
        {
            var claims = new[]
            {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, isGuest ? "Guest" : "Admin"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, isGuest ? "Guest" : "Admin")
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
