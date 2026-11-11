
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SchoolAdmission.Api.Auth;

namespace SchoolAdmission.Tests;

public class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    private const string TestSecret =
        "TestOnlySecretKeyForSchoolAdmission2026_1234567890";

    private const string TestIssuer =
        "SchoolAdmission.Api";

    private const string TestAudience =
        "SchoolAdmission.Students";

    public TestApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Secret", TestSecret);
        builder.UseSetting("Jwt:Issuer", TestIssuer);
        builder.UseSetting("Jwt:Audience", TestAudience);

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            _connectionString);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    _connectionString,

                ["Jwt:Secret"] = TestSecret,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            var testConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = TestSecret,
                        ["Jwt:Issuer"] = TestIssuer,
                        ["Jwt:Audience"] = TestAudience
                    })
                .Build();

            services.RemoveAll<JwtService>();

            services.AddScoped<JwtService>(_ =>
                new JwtService(testConfiguration));

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(TestSecret)),

                            ValidateIssuer = true,
                            ValidIssuer = TestIssuer,

                            ValidateAudience = true,
                            ValidAudience = TestAudience,

                            ValidateLifetime = true,

                            ClockSkew =
                                TimeSpan.FromSeconds(30),

                            RoleClaimType = ClaimTypes.Role,

                            NameClaimType =
                                ClaimTypes.NameIdentifier
                        };
                });
        });
    }

    // ==========================================
    // GENERATE TEST JWT
    // ==========================================

    public string GenerateTestToken(
        string studentId,
        string role = "Student")
    {
        using var scope = Services.CreateScope();

        var jwtService =
            scope.ServiceProvider.GetRequiredService<JwtService>();

        return jwtService.GenerateToken(studentId, role);
    }

    // ==========================================
    // GENERATE STUDENT TOKEN
    // ==========================================

    public string GenerateStudentTestToken(string studentId)
    {
        return GenerateTestToken(studentId, "Student");
    }

    // ==========================================
    // GENERATE ADMIN TOKEN
    // ==========================================

    public string GenerateAdminTestToken(string adminId)
    {
        return GenerateTestToken(adminId, "Admin");
    }

    // ==========================================
    // GENERATE EXPIRED TOKEN
    // ==========================================

    public string GenerateExpiredTestToken(
        string studentId,
        string role = "Student")
    {
        using var scope = Services.CreateScope();

        var jwtService =
            scope.ServiceProvider.GetRequiredService<JwtService>();

        return jwtService.GenerateExpiredToken(studentId, role);
    }
}
