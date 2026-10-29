using System.Net.Http.Headers;
using DavideDiDonato.CinemaBookingSystem.API.Security;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database.Seeder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;


namespace DavideDiDonato.CinemaBookingSystem.IntegrationTests;

public class CinemaBookingSystemFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer;
    public WebApplicationFactory<Program> WebApplicationFactory { get; init; }

    public CinemaBookingSystemFixture()
    {
        _postgreSqlContainer = new PostgreSqlBuilder("postgres:16-alpine").Build();
        WebApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Development")
                .UseSetting("ConnectionStrings:DatabaseConnectionString", _postgreSqlContainer.GetConnectionString())
                .UseSetting("MockAuthentication", "true"));
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }
    
    public IServiceScope CreateScope()
    {
        IServiceScope result = WebApplicationFactory.Services.CreateScope();

        return result;
    }

    public HttpClient CreateAuthenticatedClient(int userId = 1, bool asAdmin = false)
    {
        HttpClient result = WebApplicationFactory.CreateClient();
        string token = asAdmin ? $"{userId}:{Roles.Admin}" : userId.ToString();
        result.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return result;
    }

    public async Task SeedDataAsync()
    {
        using IServiceScope scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<Seeder>().SeedData();
    }

    public async Task DisposeAsync()
    {
        await WebApplicationFactory.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
    }
}
