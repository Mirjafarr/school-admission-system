
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SchoolAdmission.Api.Auth;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // ==========================================
    // GENERATE VALID JWT
    // ==========================================

    public string GenerateToken(string studentId)
    {
        return GenerateToken(studentId, "Student");
    }

    public string GenerateToken(
        string studentId,
        string role)
    {
        if (role != "Admin" && role != "Student")
        {
            throw new ArgumentException(
                "Invalid user role.", nameof(role));
        }

        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "JWT secret is missing.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is missing.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is missing.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                studentId),

            new Claim(
                ClaimTypes.NameIdentifier,
                studentId),

            new Claim(
                ClaimTypes.Role,
                role),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // ==========================================
    // GENERATE EXPIRED JWT (TESTING)
    // ==========================================

    public string GenerateExpiredToken(string studentId)
    {
        return GenerateExpiredToken(studentId, "Student");
    }

    public string GenerateExpiredToken(
        string studentId,
        string role)
    {
        if (role != "Admin" && role != "Student")
        {
            throw new ArgumentException(
                "Invalid user role.", nameof(role));
        }

        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "JWT secret is missing.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is missing.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is missing.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                studentId),

            new Claim(
                ClaimTypes.NameIdentifier,
                studentId),

            new Claim(
                ClaimTypes.Role,
                role),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
