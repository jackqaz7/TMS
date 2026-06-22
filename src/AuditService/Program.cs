using AuditService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// AuditService is intentionally its own API and DbContext. That keeps audit
// storage owned by the audit microservice instead of leaking audit tables into
// the core TMS database.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// This points at TMSAudit, not TMSLive. A separate database is the important
// microservice learning point: the Audit Service owns its data and schema.
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Audit")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // /openapi/v1.json lets Swagger or HTTP clients discover the service contract.
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Controllers expose the REST boundary: CoreAPI sends audit events here over HTTP.
app.MapControllers();

app.Run();
