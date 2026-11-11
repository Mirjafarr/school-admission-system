
using Microsoft.AspNetCore.Identity;

namespace SchoolAdmission.Api.Auth;

public class PasswordService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string HashPassword(string studentId, string password)
    {
        return _hasher.HashPassword(studentId, password);
    }

    public bool VerifyPassword(
        string studentId,
        string password,
        string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(
            studentId,
            passwordHash,
            password
        );

        return result != PasswordVerificationResult.Failed;
    }
}
