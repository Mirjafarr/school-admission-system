
using System.Net;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class LoginRateLimitTests
{
    private readonly HttpClient _client;

    public LoginRateLimitTests(TestDatabaseFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Login_AfterFiveAttempts_ShouldReturn429()
    {
        var loginRequest = new
        {
            studentId = "RATELIMIT0000001",
            password = "WrongPassword123"
        };

        // First five attempts should reach the login endpoint.
        for (var i = 1; i <= 5; i++)
        {
            var response = await _client.PostAsJsonAsync(
                "/auth/login",
                loginRequest);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        // Sixth attempt should be rate limited.
        var sixthResponse = await _client.PostAsJsonAsync(
            "/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            sixthResponse.StatusCode);
    }
}
