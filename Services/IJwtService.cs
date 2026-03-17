using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public interface IJwtService {
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
}

public class JwtService(IConfiguration config) : IJwtService {
    private readonly string _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "UserManagementAPI";
    private readonly string _audience = config["Jwt:Audience"] ?? "UserManagementAPI";
    private readonly int _expirationMinutes = int.Parse(config["Jwt:ExpirationMinutes"] ?? "60");

    public string GenerateAccessToken(User user) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken() {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token) {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        try {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = false // Allow expired tokens for refresh flow
            }, out _);

            return principal;
        } catch {
            return null;
        }
    }

    public int ExpirationMinutes => _expirationMinutes;
}