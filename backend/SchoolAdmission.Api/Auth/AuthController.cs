
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SchoolAdmission.Api.Auth;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly PasswordService _passwordService;
    private readonly JwtService _jwtService;

    private const int MaxFailedAttempts = 5;

    private static readonly TimeSpan LockoutDuration =
        TimeSpan.FromMinutes(15);

    public AuthController(
        NpgsqlDataSource dataSource,
        PasswordService passwordService,
        JwtService jwtService)
    {
        _dataSource = dataSource;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    // ==========================================
    // 1. STUDENT REGISTRATION
    // POST /auth/register
    // ==========================================

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        var studentId = request.StudentId?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length > 17)
        {
            return BadRequest(new
            {
                error = "A valid student ID is required."
            });
        }

        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 8 ||
            password.Length > 128)
        {
            return BadRequest(new
            {
                error = "Password must be between 8 and 128 characters."
            });
        }

        var passwordHash =
            _passwordService.HashPassword(studentId, password);

        const string sql = """
            INSERT INTO students (student_id, password_hash, role)
            VALUES (@studentId, @passwordHash, 'Student')
            RETURNING id, student_id, created_at;
            """;

        try
        {
            await using var connection =
                await _dataSource.OpenConnectionAsync();

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "studentId", studentId);

            command.Parameters.AddWithValue(
                "passwordHash", passwordHash);

            await using var reader =
                await command.ExecuteReaderAsync();

            await reader.ReadAsync();

            return StatusCode(201, new
            {
                id = reader.GetGuid(0),
                studentId = reader.GetString(1),
                createdAt = reader.GetDateTime(2)
            });
        }
        catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new
            {
                error = "This student ID is already registered."
            });
        }
    }

    // ==========================================
    // 2. STUDENT LOGIN
    // POST /auth/login
    // ==========================================

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var studentId = request.StudentId?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length > 17 ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length > 128)
        {
            return BadRequest(new
            {
                error = "Valid student ID and password are required."
            });
        }

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        // ==========================================
        // 2.1 LOCK STUDENT DATABASE ROW
        // ==========================================

        const string selectSql = """
            SELECT
                password_hash,
                failed_login_attempts,
                locked_until, role
            FROM students
            WHERE student_id = @studentId
            FOR UPDATE;
            """;

        string? passwordHash;
        string role;
        int failedAttempts;
        DateTime? lockedUntil;

        await using (var command = new NpgsqlCommand(
            selectSql, connection, transaction))
        {
            command.Parameters.AddWithValue(
                "studentId", studentId);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                await reader.CloseAsync();
                await transaction.CommitAsync();

                return Unauthorized(new
                {
                    error = "Invalid student ID or password."
                });
            }

            passwordHash = reader.GetString(0);
            failedAttempts = reader.GetInt32(1);

            lockedUntil = reader.IsDBNull(2)
                ? null
              : reader.GetDateTime(2);

                role = reader.GetString(3);
        }

        // ==========================================
        // 2.2 CHECK ACCOUNT LOCKOUT
        // ==========================================

        var now = DateTime.UtcNow;

        if (lockedUntil.HasValue &&
            lockedUntil.Value > now)
        {
            await transaction.CommitAsync();

            return StatusCode(423, new
            {
                error = "Account temporarily locked due to repeated failed login attempts.",
                lockedUntil = lockedUntil.Value
            });
        }

        // ==========================================
        // 2.3 RESET EXPIRED LOCKOUT
        // ==========================================

        if (lockedUntil.HasValue)
        {
            const string resetExpiredLockSql = """
                UPDATE students
                SET
                    failed_login_attempts = 0,
                    locked_until = NULL
                WHERE student_id = @studentId;
                """;

            await using var resetCommand =
                new NpgsqlCommand(
                    resetExpiredLockSql,
                    connection,
                    transaction);

            resetCommand.Parameters.AddWithValue(
                "studentId", studentId);

            await resetCommand.ExecuteNonQueryAsync();

            failedAttempts = 0;
        }

        // ==========================================
        // 2.4 VERIFY PASSWORD
        // ==========================================

        var passwordValid =
            _passwordService.VerifyPassword(
                studentId,
                password,
                passwordHash!);

        if (!passwordValid)
        {
            var newFailedAttempts = failedAttempts + 1;

            var shouldLock =
                newFailedAttempts >= MaxFailedAttempts;

            DateTime? newLockedUntil = shouldLock
                ? now.Add(LockoutDuration)
                : null;

            // ==========================================
            // 2.5 RECORD FAILED LOGIN
            // ==========================================

            const string updateFailedLoginSql = """
                UPDATE students
                SET
                    failed_login_attempts = @attempts,
                    locked_until = @lockedUntil
                WHERE student_id = @studentId;
                """;

            await using var updateCommand =
                new NpgsqlCommand(
                    updateFailedLoginSql,
                    connection,
                    transaction);

            updateCommand.Parameters.AddWithValue(
                "attempts", newFailedAttempts);

            updateCommand.Parameters.AddWithValue(
                "lockedUntil",
                (object?)newLockedUntil ?? DBNull.Value);

            updateCommand.Parameters.AddWithValue(
                "studentId", studentId);

            await updateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            if (shouldLock)
            {
                return StatusCode(423, new
                {
                    error = "Account locked after five failed login attempts.",
                    lockedUntil = newLockedUntil
                });
            }

            return Unauthorized(new
            {
                error = "Invalid student ID or password.",
                remainingAttempts =
                    MaxFailedAttempts - newFailedAttempts
            });
        }

        // ==========================================
        // 2.6 RESET FAILED ATTEMPTS ON SUCCESS
        // ==========================================

        const string resetFailedAttemptsSql = """
            UPDATE students
            SET
                failed_login_attempts = 0,
                locked_until = NULL
            WHERE student_id = @studentId;
            """;

        await using (var resetCommand = new NpgsqlCommand(
            resetFailedAttemptsSql,
            connection,
            transaction))
        {
            resetCommand.Parameters.AddWithValue(
                "studentId", studentId);

            await resetCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        // ==========================================
        // 2.7 GENERATE JWT
        // ==========================================

        var token = _jwtService.GenerateToken(
         studentId,
         role);
  
        return Ok(new AuthResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: 1800
        ));
    }
}
