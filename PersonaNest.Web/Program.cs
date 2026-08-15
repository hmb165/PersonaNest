using PersonaNest.Services;
using PersonaNest.Services.Interfaces;
using PersonaNest.Web.Hubs;
using PersonaNest.Web.Realtime;
using Serilog;

// Bootstrap logger: catches anything that happens before the full configuration-driven logger
// below is built (e.g. a failure reading configuration itself). Replaced once the host starts.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// §24: every ILogger<T> in every layer (Services, Infrastructure, Web) is routed through Serilog
// from here on - no code elsewhere needs to reference Serilog directly. Configuration lives in
// the "Serilog" section of appsettings.json / appsettings.Development.json (documented in
// README.md), read via ReadFrom.Configuration so sinks/levels can change without a recompile.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

// §12/Phase 15: real-time notifications. The hub and its IHubContext-backed broadcaster live in
// Web (the only layer allowed to reference ASP.NET Core hosting types) - Services depends only on
// the INotificationBroadcaster abstraction it defines.
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "PersonaNest API",
        Version = "v1",
        Description = "Read-oriented REST API for external consumers (§25). Backed by the same " +
            "Services layer as the MVC site; every endpoint returns DTOs, never EF entities."
    });

    // The MVC controllers also carry explicit attribute routes (e.g. Admin/Users/Ban/{id}), which
    // ApiExplorer would otherwise surface here too - they return views/redirects, not JSON, and
    // are not part of the REST API. Scope this document to the api/ prefix only.
    options.DocInclusionPredicate((_, apiDescription) =>
        apiDescription.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true);
});

// The single registration call into the rest of the application. AddApplicationServices calls
// AddInfrastructure internally, so PersonaNest.Web never references PersonaNest.Infrastructure
// and the approved dependency direction Web -> Services -> Infrastructure holds.
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// One line per HTTP request at Information level (method, path, status, elapsed) - separate from
// the more targeted logging inside auth/moderation/background-service code (§24).
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // §25: Swagger only in Development, matching the project's existing convention of gating
    // dev-only surfaces (exception pages, seeded demo accounts) behind IsDevelopment().
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PersonaNest API v1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Applies migrations in Development, then seeds the three Identity roles. Demo accounts are
// seeded in Development only, with passwords read from configuration (§14).
await app.Services.InitializeDatabaseAsync(app.Environment.IsDevelopment());

try
{
    Log.Information("PersonaNest starting up.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PersonaNest terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
