
namespace SchoolAdmission.Api.Auth;

public record RegisterRequest(
    string StudentId,
    string Password
);

public record LoginRequest(
    string StudentId,
    string Password
);

public record AuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn
);
