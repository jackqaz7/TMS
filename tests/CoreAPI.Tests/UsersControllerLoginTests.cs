using CoreAPI.Controllers;
using CoreAPI.Data;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Xunit;

namespace CoreAPI.Tests;

public class UsersControllerLoginTests
{
    [Fact]
    public async Task Login_ReturnsJwtToken_WhenCredentialsAreValid()
    {
        await using var provider = CreateServiceProvider();
        await SeedUserAsync(provider, username: "arjun", password: "pass123");

        var controller = provider.GetRequiredService<UsersController>();

        var result = await controller.Login(new LoginDto
        {
            Username = "arjun",
            Password = "pass123"
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var loginResponse = JsonSerializer.Deserialize<LoginResponseForTest>(json, JsonOptions);

        Assert.False(string.IsNullOrWhiteSpace(loginResponse?.Token));

        // Reading the JWT proves the login endpoint created a real token, not just any string.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(loginResponse.Token);
        Assert.Equal("TMS.Tests", jwt.Issuer);

        var auditClient = provider.GetRequiredService<FakeAuditClient>();
        Assert.Contains(auditClient.Events, e => e.EventType == "LoginSucceeded");
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        await using var provider = CreateServiceProvider();
        await SeedUserAsync(provider, username: "arjun", password: "pass123");

        var controller = provider.GetRequiredService<UsersController>();

        var result = await controller.Login(new LoginDto
        {
            Username = "arjun",
            Password = "wrong-password"
        });

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid username or password", unauthorizedResult.Value);

        var auditClient = provider.GetRequiredService<FakeAuditClient>();
        Assert.Contains(auditClient.Events, e => e.EventType == "LoginFailed");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // The test uses the same DI idea as Program.cs, but swaps SQL Server for an
        // in-memory database so login can be tested without a real database connection.
        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase($"auth-tests-{Guid.NewGuid()}"));

        services.AddSingleton<IConfiguration>(_ =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ThisIsASecretKeyForJwtToken12345",
                    ["Jwt:Issuer"] = "TMS.Tests",
                    ["Jwt:Audience"] = "TMS.Tests"
                })
                .Build());

        services.AddSingleton<FakeAuditClient>();
        services.AddSingleton<IAuditClient>(provider =>
            provider.GetRequiredService<FakeAuditClient>());

        services.AddTransient<UsersController>();

        return services.BuildServiceProvider();
    }

    private static async Task SeedUserAsync(
        IServiceProvider provider,
        string username,
        string password)
    {
        var context = provider.GetRequiredService<AuthDbContext>();

        context.AppUsers.Add(new User
        {
            Username = username,
            PasswordHash = password,
            Role = "TreasuryUser"
        });

        await context.SaveChangesAsync();
    }

    private sealed class FakeAuditClient : IAuditClient
    {
        public List<AuditEventRequest> Events { get; } = new();

        public Task RecordAsync(
            AuditEventRequest auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class LoginResponseForTest
    {
        public string Token { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
