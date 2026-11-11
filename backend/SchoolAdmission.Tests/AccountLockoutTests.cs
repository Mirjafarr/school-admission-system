
using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class AccountLockoutTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    private const string CorrectPassword = "CorrectPassword123";

    public AccountLockoutTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;

        // Each test gets an independent application instance
        // and therefore an independent IP rate-limit counter.
        _factory = new TestApiFactory(fixture.ConnectionString);

        _client = _factory.CreateClient();
    }

    // ==========================================
    // TEST 1: FIVE FAILED ATTEMPTS
    // ==========================================

    [Fact]
    public async Task Login_FiveFailedAttempts_ShouldLockAccount()
    {
        var studentId = GenerateStudentId();

        await RegisterStudent(studentId);

        // First four attempts should return 401.
        for (var i = 1; i <= 4; i++)
        {
            var response = await Login(
                studentId,
                "WrongPassword123");

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        // Fifth attempt should lock the account.
        var fifthResponse = await Login(
            studentId,
            "WrongPassword123");

        Assert.Equal(
            HttpStatusCode.Locked,
            fifthResponse.StatusCode);

        // Verify lockout was persisted in PostgreSQL.
        var account = await GetAccountSecurityState(studentId);

        Assert.Equal(5, account.FailedAttempts);
        Assert.NotNull(account.LockedUntil);
        Assert.True(account.LockedUntil > DateTime.UtcNow);
    }

    // ==========================================
    // TEST 2: CORRECT PASSWORD WHILE LOCKED
    // ==========================================

    [Fact]
    public async Task Login_WhileAccountLocked_ShouldReturn423()
    {
        var studentId = GenerateStudentId();

        await RegisterStudent(studentId);

        await SetAccountLockout(
            studentId,
            failedAttempts: 5,
            lockedUntil: DateTime.UtcNow.AddMinutes(15));

        var response = await Login(
            studentId,
            CorrectPassword);

        Assert.Equal(
            HttpStatusCode.Locked,
            response.StatusCode);
    }

    // ==========================================
    // TEST 3: SUCCESSFUL LOGIN RESETS COUNTER
    // ==========================================

    [Fact]
    public async Task Login_SuccessfulLogin_ShouldResetFailedAttempts()
    {
        var studentId = GenerateStudentId();

        await RegisterStudent(studentId);

        // Generate three failed attempts.
        for (var i = 1; i <= 3; i++)
        {
            var response = await Login(
                studentId,
                "WrongPassword123");

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        var beforeLogin =
            await GetAccountSecurityState(studentId);

        Assert.Equal(3, beforeLogin.FailedAttempts);

        // Correct password should succeed.
        var successfulResponse = await Login(
            studentId,
            CorrectPassword);

        Assert.Equal(
            HttpStatusCode.OK,
            successfulResponse.StatusCode);

        var afterLogin =
            await GetAccountSecurityState(studentId);

        Assert.Equal(0, afterLogin.FailedAttempts);
        Assert.Null(afterLogin.LockedUntil);
    }

    // ==========================================
    // TEST 4: EXPIRED LOCKOUT ALLOWS LOGIN
    // ==========================================

    [Fact]
    public async Task Login_AfterLockoutExpires_ShouldAllowLogin()
    {
        var studentId = GenerateStudentId();

        await RegisterStudent(studentId);

        // Simulate an expired lockout.
        await SetAccountLockout(
            studentId,
            failedAttempts: 5,
            lockedUntil: DateTime.UtcNow.AddMinutes(-1));

        var response = await Login(
            studentId,
            CorrectPassword);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var account =
            await GetAccountSecurityState(studentId);

        Assert.Equal(0, account.FailedAttempts);
        Assert.Null(account.LockedUntil);
    }

    // ==========================================
    // HELPER: REGISTER STUDENT
    // ==========================================

    private async Task RegisterStudent(string studentId)
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                studentId,
                password = CorrectPassword
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    // ==========================================
    // HELPER: LOGIN
    // ==========================================

    private async Task<HttpResponseMessage> Login(
        string studentId,
        string password)
    {
        return await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                studentId,
                password
            });
    }

    // ==========================================
    // HELPER: GENERATE UNIQUE STUDENT ID
    // ==========================================

    private static string GenerateStudentId()
    {
        return "L" + Guid.NewGuid().ToString("N")[..16];
    }

    // ==========================================
    // HELPER: SET ACCOUNT LOCKOUT IN DATABASE
    // ==========================================

    private async Task SetAccountLockout(
        string studentId,
        int failedAttempts,
        DateTime lockedUntil)
    {
        await using var connection =
            new NpgsqlConnection(_fixture.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
            UPDATE students
            SET
                failed_login_attempts = @attempts,
                locked_until = @lockedUntil
            WHERE student_id = @studentId;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "attempts", failedAttempts);

        command.Parameters.AddWithValue(
            "lockedUntil", lockedUntil.ToUniversalTime());

        command.Parameters.AddWithValue(
            "studentId", studentId);

        var rows = await command.ExecuteNonQueryAsync();

        Assert.Equal(1, rows);
    }

    // ==========================================
    // HELPER: READ ACCOUNT SECURITY STATE
    // ==========================================

    private async Task<(int FailedAttempts, DateTime? LockedUntil)>
        GetAccountSecurityState(string studentId)
    {
        await using var connection =
            new NpgsqlConnection(_fixture.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
            SELECT failed_login_attempts, locked_until
            FROM students
            WHERE student_id = @studentId;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "studentId", studentId);

        await using var reader =
            await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());

        var attempts = reader.GetInt32(0);

        DateTime? lockedUntil = reader.IsDBNull(1)
            ? null
            : reader.GetDateTime(1);

        return (attempts, lockedUntil);
    }

    // ==========================================
    // CLEANUP
    // ==========================================

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
