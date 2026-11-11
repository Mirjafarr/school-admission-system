
using Npgsql;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Tests;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    public string ConnectionString =>
    _postgres.GetConnectionString();
    public TestApiFactory Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public TestDatabaseFixture()
{
    _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("schooladmission_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();
}

    public async Task InitializeAsync()
    {
        // Start isolated PostgreSQL container
        await _postgres.StartAsync();

        // Initialize database schema
        var schemaPath = Path.Combine(
            AppContext.BaseDirectory,
            "Database",
            "schema.sql");

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException(
                "Test database schema was not found.",
                schemaPath);
        }

        var schema = await File.ReadAllTextAsync(schemaPath);

        await using (var connection =
            new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();

            await using var command =
                new NpgsqlCommand(schema, connection);

            await command.ExecuteNonQueryAsync();
        }

        // Start the application using the test database
        Factory = new TestApiFactory(
            _postgres.GetConnectionString());

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();

        await _postgres.DisposeAsync();
    }
}
