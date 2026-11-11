
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class ConcurrencyTests
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ConcurrencyTests(TestDatabaseFixture fixture)
    {
        _factory = fixture.Factory;
        _client = fixture.Client;
    }

    // ==========================================
    // HELPER: CREATE CLASS AS ADMIN
    // ==========================================

    private async Task<string> CreateTestClass(int seats)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/classes");

        var token = _factory.GenerateAdminTestToken(
            "ADMIN00000000001");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(new
        {
            name = $"Concurrency Test {Guid.NewGuid()}",
            totalSeats = seats
        });

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ClassResponse>();

        return result!.Id;
    }

    // ==========================================
    // TEST: PREVENT CONCURRENT OVERBOOKING
    // ==========================================

    [Fact]
    public async Task ConcurrentReservations_ShouldNeverOverbookSeats()
    {
        // Arrange
        var classId = await CreateTestClass(5);

        const int studentCount = 10;

        // Act: 10 students simultaneously request seats.
        var tasks = Enumerable.Range(1, studentCount)
            .Select(i => CreateReservation(
                classId,
                $"CN{i:D9}"));

        var responses = await Task.WhenAll(tasks);

        // Unexpected responses
        var unexpected = responses
            .Where(r =>
                r.StatusCode != HttpStatusCode.Created &&
                r.StatusCode != HttpStatusCode.Conflict)
            .Select(r => $"{(int)r.StatusCode} {r.StatusCode}")
            .ToList();

        Assert.True(
            unexpected.Count == 0,
            $"Unexpected HTTP responses: {string.Join(", ", unexpected)}");

        // Exactly 5 reservations should succeed.
        var successful = responses.Count(
            r => r.StatusCode == HttpStatusCode.Created);

        var rejected = responses.Count(
            r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(5, successful);
        Assert.Equal(5, rejected);

        // Verify actual database seat counts.
        var detailsResponse = await _client.GetAsync(
            $"/classes/{classId}");

        Assert.Equal(
            HttpStatusCode.OK,
            detailsResponse.StatusCode);

        var details = await detailsResponse.Content
            .ReadFromJsonAsync<ClassDetailsResponse>();

        Assert.NotNull(details);

        Assert.Equal(5, details.HeldSeats);
        Assert.Equal(0, details.ConfirmedSeats);
        Assert.Equal(0, details.AvailableSeats);

        // Dispose responses.
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    // ==========================================
    // HELPER: CREATE RESERVATION AS STUDENT
    // ==========================================

    private async Task<HttpResponseMessage> CreateReservation(
        string classId,
        string studentId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/classes/{classId}/holds");

        var token = _factory.GenerateStudentTestToken(studentId);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    private record ClassResponse(
        string Id,
        string Name,
        int TotalSeats);

    private record ClassDetailsResponse(
        string Id,
        string Name,
        int TotalSeats,
        long HeldSeats,
        long ConfirmedSeats,
        long AvailableSeats);
}
