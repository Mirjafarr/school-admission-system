
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Npgsql;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class ExpirationTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ExpirationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _factory = fixture.Factory;
        _client = fixture.Client;
    }

    // ==========================================
    // TEST 1: EXPIRED RESERVATION CANNOT CONFIRM
    // ==========================================

    [Fact]
    public async Task ConfirmHold_ShouldRejectExpiredReservation()
    {
        var classId = await CreateTestClass();

        var studentId = "EXPIRE0000000001";

        var holdId = await CreateExpiredHold(
            classId, studentId);

        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds/{holdId}/confirm",
            studentId);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    // ==========================================
    // TEST 2: EXPIRED RESERVATION CANNOT CANCEL
    // ==========================================

    [Fact]
    public async Task CancelHold_ShouldRejectExpiredReservation()
    {
        var classId = await CreateTestClass();

        var studentId = "EXPIRE0000000002";

        var holdId = await CreateExpiredHold(
            classId, studentId);

        using var request = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/holds/{holdId}",
            studentId);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    // ==========================================
    // HELPER: AUTHENTICATED STUDENT REQUEST
    // ==========================================

    private HttpRequestMessage AuthenticatedRequest(
        HttpMethod method,
        string url,
        string studentId)
    {
        var request = new HttpRequestMessage(method, url);

        var token =
            _factory.GenerateStudentTestToken(studentId);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    // ==========================================
    // HELPER: CREATE CLASS AS ADMIN
    // ==========================================

    private async Task<string> CreateTestClass()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/classes");

        var adminToken =
            _factory.GenerateAdminTestToken("ADMIN00000000001");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        request.Content = JsonContent.Create(new
        {
            name = $"Expiration Test {Guid.NewGuid()}",
            totalSeats = 5
        });

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ClassResponse>();

        return result!.Id;
    }

    // ==========================================
    // HELPER: INSERT EXPIRED HOLD
    // ==========================================

    private async Task<Guid> CreateExpiredHold(
        string classId,
        string studentId)
    {
        var rawClassId = Guid.Parse(classId.Substring(2));

        await using var connection =
            new NpgsqlConnection(_fixture.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
            INSERT INTO admission_holds
                (class_id, student_id, status, expires_at)
            VALUES
                (@classId, @studentId, 'HELD',
                 NOW() - INTERVAL '1 minute')
            RETURNING id;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("classId", rawClassId);
        command.Parameters.AddWithValue("studentId", studentId);

        return (Guid)(await command.ExecuteScalarAsync())!;
    }

    private record ClassResponse(
        string Id,
        string Name,
        int TotalSeats);
}
