
using System.Net;
using System.Net.Http.Headers;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class JwtSecurityTests
{
    private readonly HttpClient _client;
    private readonly TestApiFactory _factory;

    public JwtSecurityTests(TestDatabaseFixture fixture)
    {
        _client = fixture.Client;
        _factory = fixture.Factory;
    }

    // ==========================================
    // TEST 1: MISSING TOKEN
    // ==========================================

    [Fact]
    public async Task CreateHold_WithoutToken_ShouldReturn401()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/classes/c_00000000-0000-0000-0000-000000000001/holds");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ==========================================
    // TEST 2: INVALID TOKEN
    // ==========================================

    [Fact]
    public async Task CreateHold_WithInvalidToken_ShouldReturn401()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/classes/c_00000000-0000-0000-0000-000000000001/holds");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "this.is.an.invalid.jwt");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ==========================================
    // TEST 3: INVALID SIGNATURE
    // ==========================================

    [Fact]
    public async Task CreateHold_WithWrongSignature_ShouldReturn401()
    {
        var validToken = _factory.GenerateTestToken(
            "SECURITY00000001");

        var parts = validToken.Split('.');

        Assert.Equal(3, parts.Length);

        parts[2] = (parts[2][0] == 'A' ? "B" : "A")
            + parts[2].Substring(1);

        var invalidToken = string.Join(".", parts);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/classes/c_00000000-0000-0000-0000-000000000001/holds");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                invalidToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ==========================================
    // TEST 4: MALFORMED TOKEN
    // ==========================================

    [Fact]
    public async Task CreateHold_WithMalformedToken_ShouldReturn401()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/classes/c_00000000-0000-0000-0000-000000000001/holds");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "not-a-valid-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ==========================================
    // TEST 5: EXPIRED TOKEN
    // ==========================================

    [Fact]
    public async Task CreateHold_WithExpiredToken_ShouldReturn401()
    {
        var token = _factory.GenerateExpiredTestToken(
            "SECURITY00000003");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/classes/c_00000000-0000-0000-0000-000000000001/holds");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}
