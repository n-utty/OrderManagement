using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace OrderManagement.Api.Tests.Support;

// Runs the real API, startup migrations included, against a private in-memory SQLite database.
// The open connection keeps that database alive for as long as the factory exists.
public sealed class OrderManagementApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=api-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public OrderManagementApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:OrderManagement", _connectionString);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
