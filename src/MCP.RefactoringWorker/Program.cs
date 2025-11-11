using Hangfire;
using Hangfire.SqlServer;
using MCP.RefactoringWorker;
using MCP.RefactoringWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure plugin directory
var pluginDirectory = builder.Configuration.GetValue<string>("PluginDirectory")
                      ?? Path.Combine(AppContext.BaseDirectory, "plugins");

// Register the plugin loader as a singleton
var pluginLoader = new PluginLoader(
    builder.Services.BuildServiceProvider().GetRequiredService<ILogger<PluginLoader>>());
pluginLoader.LoadPlugins(pluginDirectory);
builder.Services.AddSingleton(pluginLoader);

// Register the refactoring service
builder.Services.AddSingleton<IRefactoringService, RefactoringService>();

// Configure Hangfire with SQL Server storage
// This provides the persistent job queue as specified in Section 6.4
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

// Add the Hangfire server
// This makes this service a "worker" that processes jobs from the queue
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = builder.Configuration.GetValue<int>("Hangfire:WorkerCount", Environment.ProcessorCount);
    options.Queues = new[] { "refactoring", "default" };
});

// Add the background worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=================================================");
logger.LogInformation("MCP RefactoringWorker Service Starting");
logger.LogInformation("=================================================");
logger.LogInformation("Plugin Directory: {PluginDirectory}", pluginDirectory);
logger.LogInformation("Hangfire Connection: {Connection}",
    hangfireConnectionString.Split(';')[0]); // Only log the server, not credentials
logger.LogInformation("Worker Count: {WorkerCount}",
    builder.Configuration.GetValue<int>("Hangfire:WorkerCount", Environment.ProcessorCount));
logger.LogInformation("Loaded Plugins: {Plugins}",
    string.Join(", ", pluginLoader.GetProviderNames()));
logger.LogInformation("=================================================");

host.Run();
