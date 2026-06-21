using CoreAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers are the REST API entry points. ASP.NET Core will scan the project
// for classes ending in Controller and map their actions through endpoint routing.
builder.Services.AddControllers();

// OpenAPI exposes machine-readable API metadata in development. This helps tools
// such as Swagger/HTTP clients understand available endpoints and request shapes.
builder.Services.AddOpenApi();

// AuthDbContext is intentionally separate from TmsDbContext so login/security data
// can evolve independently from treasury trade data.
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Auth")));

// TmsDbContext is the treasury database boundary. EF Core creates SQL queries for
// DbSet<Trade> operations used by TreasuryController.
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TMS")));

// JWT bearer authentication tells ASP.NET Core how to read and validate the
// Authorization: Bearer <token> header sent by the WPF client after login.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // These checks make sure the token was issued by this API, intended for this
        // audience, not expired, and signed with our configured secret key.
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("StartupWarmup");

    var sw = Stopwatch.StartNew();

    try
    {
        var authDbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        await authDbContext.AppUsers
            .AsNoTracking()
            .AnyAsync();

        logger.LogInformation("Auth DB warm-up took {ElapsedMs} ms", sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Auth DB warm-up failed. First login may still be slow.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Order matters: Authentication identifies the user first; Authorization then
// checks whether that user is allowed to execute [Authorize] endpoints.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
