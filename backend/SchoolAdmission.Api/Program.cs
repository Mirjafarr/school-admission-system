
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SchoolAdmission.Api.Auth;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// ==========================================
// 1. DATABASE CONFIGURATION
// ==========================================

builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    var connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Database connection string is missing.");

    return NpgsqlDataSource.Create(connectionString);
});

// ==========================================
// 2. AUTHENTICATION SERVICES
// ==========================================

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "JWT secret is missing.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is missing.");

var signingKey = Encoding.UTF8.GetBytes(jwtSecret);

if (signingKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT secret must contain at least 32 bytes.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(signingKey),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                // Map JWT role claims to ASP.NET Core roles.
                RoleClaimType =
                    System.Security.Claims.ClaimTypes.Role,

                NameClaimType =
                    System.Security.Claims.ClaimTypes.NameIdentifier
            };
    });

// ==========================================
// 3. AUTHORIZATION POLICIES
// ==========================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });

    options.AddPolicy("StudentOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Student");
    });
});

// ==========================================
// 4. LOGIN RATE LIMITING
// ==========================================

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("LoginPolicy", context =>
    {
        var ipAddress =
            context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.RejectionStatusCode = 429;

    options.OnRejected = async (
        context,
        cancellationToken) =>
    {
        context.HttpContext.Response.ContentType =
            "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                error = "Too many login attempts. Please try again later."
            },
            cancellationToken);
    };
});

// ==========================================
// 5. CONTROLLERS AND SWAGGER
// ==========================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});


// ==========================================
// 6. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 7. HTTP REQUEST PIPELINE
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// ==========================================
// 8. DATABASE HEALTH CHECK
// ==========================================

app.MapGet("/db-health", async (
    NpgsqlDataSource dataSource) =>
{
    await using var connection =
        await dataSource.OpenConnectionAsync();

    await using var command =
        new NpgsqlCommand("SELECT 1", connection);

    var result = await command.ExecuteScalarAsync();

    return Results.Ok(new
    {
        status = "connected",
        database = "PostgreSQL",
        result
    });
});

// ==========================================
// 9. START APPLICATION
// ==========================================

app.Run();

public partial class Program { }
