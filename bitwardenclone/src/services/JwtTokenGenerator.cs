using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using bitwardenclone.src.models;
using Microsoft.IdentityModel.Tokens;

namespace bitwardenclone.src.services;

public class JwtTokenGenerator(IConfiguration configuration)
{
    public string GenerateToken(User user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found.");
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not found.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not found.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public User? ExtractFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out _);

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);

            if (userId is null || email is null)
                return null;

            return new User
            {
                Id = Guid.Parse(userId),
                Email = email,
                MasterPasswordHash = ""
            };
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
