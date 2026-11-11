
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

[Collection("SchoolAdmission Integration Tests")]
public class ClassesApiTests
{
    private readonly HttpClient _client;
    private readonly TestApiFactory _factory;

    public ClassesApiTests(TestDatabaseFixture fixture)
    {
        _client = fixture.Client;
        _factory = fixture.Factory;
    }

    private HttpRequestMessage CreateAdminRequest(
        HttpMethod method,
        string url)
    {
        var request = new HttpRequestMessage(method, url);

        var token = _factory.GenerateAdminTestToken(
          "ADMIN00000000001");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    // ==========================================
    // TEST 1: CREATE CLASS
    // ==========================================

    [Fact]
    public async Task CreateClass_ShouldReturnCreatedClass()
    {
        var requestBody = new
        {
            name = $"Integration Test Class {Guid.NewGuid()}",
            totalSeats = 10
        };

        using var request = CreateAdminRequest(
            HttpMethod.Post,
            "/classes");

        request.Content = JsonContent.Create(requestBody);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<ClassResponse>();

        Assert.NotNull(result);
        Assert.StartsWith("c_", result.Id);
        Assert.Equal(requestBody.name, result.Name);
        Assert.Equal(10, result.TotalSeats);
    }

    // ==========================================
    // TEST 2: GET ALL CLASSES
    // ==========================================

    [Fact]
    public async Task GetClasses_ShouldReturnExistingClasses()
    {
        var className = $"GET Test Class {Guid.NewGuid()}";

        var requestBody = new
        {
            name = className,
            totalSeats = 15
        };

        using var createRequest = CreateAdminRequest(
            HttpMethod.Post,
            "/classes");

        createRequest.Content = JsonContent.Create(requestBody);

        var createResponse =
            await _client.SendAsync(createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var response = await _client.GetAsync("/classes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var classes =
            await response.Content.ReadFromJsonAsync<List<ClassResponse>>();

        Assert.NotNull(classes);

        var createdClass = Assert.Single(
            classes,
            c => c.Name == className);

        Assert.Equal(15, createdClass.TotalSeats);
    }

    private record ClassResponse(
        string Id,
        string Name,
        int TotalSeats);
}
