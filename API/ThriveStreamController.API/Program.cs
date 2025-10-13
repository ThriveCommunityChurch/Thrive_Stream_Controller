using Microsoft.EntityFrameworkCore;
using Serilog;
using ThriveStreamController.API.Hubs;
using ThriveStreamController.API.Services;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Services;
using ThriveStreamController.Data;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/thrive-stream-controller-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Thrive Stream Controller API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Use PascalCase for JSON serialization (standard for .NET APIs)
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add SignalR with PascalCase JSON serialization
    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            // Use PascalCase for JSON serialization (standard for .NET APIs)
            options.PayloadSerializerOptions.PropertyNamingPolicy = null;
        });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp", policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Configure SQLite Database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=thrivestream.db";

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));

    // Register application services
    builder.Services.AddSingleton<IOBSService, OBSService>();
    builder.Services.AddHostedService<OBSEventBroadcaster>();

    // Register media status tracking services
    builder.Services.AddSingleton<MediaStatusTracker>();
    builder.Services.AddSingleton<MediaStatusBroadcaster>();
    builder.Services.AddHostedService<MediaStatusTrackerHostedService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();

    app.UseCors("AllowReactApp");

    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<OBSHub>("/hubs/obs");

    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        Log.Information("Database initialized");
    }

    Log.Information("Thrive Stream Controller API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
