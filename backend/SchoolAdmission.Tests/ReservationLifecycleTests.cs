
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class ReservationLifecycleTests
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ReservationLifecycleTests(TestDatabaseFixture fixture)
    {
        _factory = fixture.Factory;
        _client = fixture.Client;
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
            name = $"Lifecycle Test {Guid.NewGuid()}",
            totalSeats = 5
        });

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ClassResponse>();

        return result!.Id;
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
    // HELPER: CREATE HOLD AS STUDENT
    // ==========================================

    private async Task<Guid> CreateHold(
        string classId,
        string studentId)
    {
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds",
            studentId);

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<HoldResponse>();

        return result!.Id;
    }

    // ==========================================
    // TEST 1: CONFIRM RESERVATION
    // ==========================================

    [Fact]
    public async Task ConfirmHold_ShouldChangeStatusToConfirmed()
    {
        var classId = await CreateTestClass();

        var studentId = "LIFECYCLE0000001";

        var holdId = await CreateHold(classId, studentId);

        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds/{holdId}/confirm",
            studentId);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ConfirmationResponse>();

        Assert.NotNull(result);
        Assert.Equal("CONFIRMED", result.Status);
        Assert.Equal(studentId, result.StudentId);
        Assert.NotNull(result.ConfirmedAt);
    }

    // ==========================================
    // TEST 2: CANCEL RESERVATION
    // ==========================================

    [Fact]
    public async Task CancelHold_ShouldReleaseReservation()
    {
        var classId = await CreateTestClass();

        var studentId = "LIFECYCLE0000002";

        var holdId = await CreateHold(classId, studentId);

        using var cancelRequest = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/holds/{holdId}",
            studentId);

        var response = await _client.SendAsync(cancelRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        // Verify reservation history.
        using var historyRequest = AuthenticatedRequest(
            HttpMethod.Get,
            "/me/holds",
            studentId);

        var historyResponse =
            await _client.SendAsync(historyRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            historyResponse.StatusCode);

        var history = await historyResponse.Content
            .ReadFromJsonAsync<List<HoldHistoryResponse>>();

        Assert.NotNull(history);

        var cancelledHold = Assert.Single(
            history,
            h => h.Id == holdId);

        Assert.Equal("CANCELLED", cancelledHold.Status);

        // Verify that the seat has been released.
        var classResponse = await _client.GetAsync(
            $"/classes/{classId}");

        Assert.Equal(
            HttpStatusCode.OK,
            classResponse.StatusCode);

        var classResult = await classResponse.Content
            .ReadFromJsonAsync<ClassDetailsResponse>();

        Assert.NotNull(classResult);
        Assert.Equal(0, classResult.HeldSeats);
        Assert.Equal(5, classResult.AvailableSeats);
    }

    private record ClassResponse(
        string Id,
        string Name,
        int TotalSeats);

    private record HoldResponse(
        Guid Id,
        string Status);

    private record ConfirmationResponse(
        Guid Id,
        string ClassId,
        string StudentId,
        string Status,
        DateTime? ConfirmedAt);

    private record HoldHistoryResponse(
        Guid Id,
        string Status);

    private record ClassDetailsResponse(
        string Id,
        string Name,
        int TotalSeats,
        long HeldSeats,
        long ConfirmedSeats,
        long AvailableSeats);
}
