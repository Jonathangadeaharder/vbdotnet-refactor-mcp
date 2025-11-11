using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCP API Gateway",
        Version = "v1",
        Description = "Mass Code Platform - Safe VB.NET Refactoring API\n\n" +
                     "This API provides asynchronous job submission for large-scale code refactoring operations. " +
                     "Submit jobs via POST /api/v1/refactoringjobs and poll for results via GET."
    });
});

// Configure Hangfire client
// The ApiGateway only needs to enqueue jobs, not process them
var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection")
                               ?? "Data Source=localhost;Initial Catalog=MCPHangfire;Integrated Security=True;TrustServerCertificate=True";

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Optional: Add Hangfire Dashboard for monitoring
// Only enable in non-production or with authentication
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "MCP Job Queue"
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("=================================================");
app.Logger.LogInformation("MCP API Gateway Starting");
app.Logger.LogInformation("=================================================");
app.Logger.LogInformation("Hangfire Dashboard: /hangfire");
app.Logger.LogInformation("Swagger UI: /swagger");
app.Logger.LogInformation("API Base: /api/v1/refactoringjobs");
app.Logger.LogInformation("=================================================");

app.Run();
