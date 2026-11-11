
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class ReservationSecurityTests
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ReservationSecurityTests(TestDatabaseFixture fixture)
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
            name = $"Security Test {Guid.NewGuid()}",
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
    // TEST 1: PREVENT DIFFERENT STUDENT CANCELLATION
    // ==========================================

    [Fact]
    public async Task CancelHold_ShouldRejectDifferentStudent()
    {
        var classId = await CreateTestClass();

        var ownerId = "SECURITY00000001";
        var attackerId = "SECURITY00000002";

        using var createRequest = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds",
            ownerId);

        var createResponse =
            await _client.SendAsync(createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var hold = await createResponse.Content
            .ReadFromJsonAsync<HoldResponse>();

        Assert.NotNull(hold);

        using var cancelRequest = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/holds/{hold.Id}",
            attackerId);

        var cancelResponse =
            await _client.SendAsync(cancelRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            cancelResponse.StatusCode);
    }

    // ==========================================
    // TEST 2: CANCEL REQUIRES AUTHENTICATION
    // ==========================================

    [Fact]
    public async Task CancelHold_ShouldRequireAuthentication()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/holds/{Guid.NewGuid()}");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ==========================================
    // TEST 3: PREVENT DIFFERENT STUDENT CONFIRMATION
    // ==========================================

    [Fact]
    public async Task ConfirmHold_ShouldRejectDifferentStudent()
    {
        var classId = await CreateTestClass();

        var ownerId = "CONFIRM000000001";
        var attackerId = "CONFIRM000000002";

        using var createRequest = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds",
            ownerId);

        var createResponse =
            await _client.SendAsync(createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var hold = await createResponse.Content
            .ReadFromJsonAsync<HoldResponse>();

        Assert.NotNull(hold);

        using var confirmRequest = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds/{hold.Id}/confirm",
            attackerId);

        var response = await _client.SendAsync(confirmRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    // ==========================================
    // TEST 4: PREVENT DOUBLE CONFIRMATION
    // ==========================================

    [Fact]
    public async Task ConfirmHold_ShouldRejectAlreadyConfirmedReservation()
    {
        var classId = await CreateTestClass();

        var studentId = "STATE00000000001";

        using var createRequest = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds",
            studentId);

        var createResponse =
            await _client.SendAsync(createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var hold = await createResponse.Content
            .ReadFromJsonAsync<HoldResponse>();

        Assert.NotNull(hold);

        using var firstConfirmation = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds/{hold.Id}/confirm",
            studentId);

        var firstResponse =
            await _client.SendAsync(firstConfirmation);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        using var secondConfirmation = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds/{hold.Id}/confirm",
            studentId);

        var secondResponse =
            await _client.SendAsync(secondConfirmation);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    // ==========================================
    // TEST 5: PREVENT DOUBLE CANCELLATION
    // ==========================================

    [Fact]
    public async Task CancelHold_ShouldRejectAlreadyCancelledReservation()
    {
        var classId = await CreateTestClass();

        var studentId = "STATE00000000002";

        using var createRequest = AuthenticatedRequest(
            HttpMethod.Post,
            $"/classes/{classId}/holds",
            studentId);

        var createResponse =
            await _client.SendAsync(createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var hold = await createResponse.Content
            .ReadFromJsonAsync<HoldResponse>();

        Assert.NotNull(hold);

        using var firstCancel = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/holds/{hold.Id}",
            studentId);

        var firstResponse =
            await _client.SendAsync(firstCancel);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        using var secondCancel = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/holds/{hold.Id}",
            studentId);

        var secondResponse =
            await _client.SendAsync(secondCancel);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
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
