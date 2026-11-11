
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class ReservationApiTests
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ReservationApiTests(TestDatabaseFixture fixture)
    {
        _factory = fixture.Factory;
        _client = fixture.Client;
    }

    private async Task<string> CreateTestClass(int seats)
{
    var request = new HttpRequestMessage(
        HttpMethod.Post,
        "/classes");

    var adminToken = _factory.GenerateTestToken(
        "ADMIN00000000001",
        "Admin");

    request.Headers.Authorization =
        new AuthenticationHeaderValue("Bearer", adminToken);

    request.Content = JsonContent.Create(new
    {
        name = $"Reservation Test {Guid.NewGuid()}",
        totalSeats = seats
    });

    using (request)
    {
        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ClassResponse>();

        return result!.Id;
    }
}
    // ==========================================
    // TEST 1: CREATE RESERVATION
    // ==========================================

    [Fact]
    public async Task CreateHold_ShouldReturnHeldReservation()
    {
        var classId = await CreateTestClass(2);

        var studentId = "TEST000000000001";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/classes/{classId}/holds");

        var token = _factory.GenerateTestToken(
            studentId,
            "Student");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<HoldResponse>();

        Assert.NotNull(result);
        Assert.Equal("HELD", result.Status);
        Assert.Equal(studentId, result.StudentId);
    }

    // ==========================================
    // TEST 2: PREVENT DUPLICATE RESERVATION
    // ==========================================

    [Fact]
    public async Task CreateHold_ShouldRejectDuplicateActiveReservation()
    {
        var classId = await CreateTestClass(2);

        var studentId = "TEST000000000002";

        var first = await CreateHold(classId, studentId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await CreateHold(classId, studentId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ==========================================
    // TEST 3: PREVENT OVERBOOKING
    // ==========================================

    [Fact]
    public async Task CreateHold_ShouldRejectWhenSeatsAreFull()
    {
        var classId = await CreateTestClass(1);

        var first = await CreateHold(
            classId, "TEST000000000003");

        var second = await CreateHold(
            classId, "TEST000000000004");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ==========================================
    // HELPER: CREATE AUTHENTICATED HOLD
    // ==========================================

    private async Task<HttpResponseMessage> CreateHold(
        string classId,
        string studentId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/classes/{classId}/holds");

        var token = _factory.GenerateTestToken(
             studentId,
             "Student");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    private record ClassResponse(
        string Id,
        string Name,
        int TotalSeats);

    private record HoldResponse(
        Guid Id,
        string ClassId,
        string StudentId,
        string Status,
        DateTime ExpiresAt);
}
