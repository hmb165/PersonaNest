using PersonaNest.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// The single registration call into the rest of the application. AddApplicationServices calls
// AddInfrastructure internally, so PersonaNest.Web never references PersonaNest.Infrastructure
// and the approved dependency direction Web -> Services -> Infrastructure holds.
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
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

// Applies migrations in Development, then seeds the three Identity roles. Demo accounts are
// seeded in Development only, with passwords read from configuration (§14).
await app.Services.InitializeDatabaseAsync(app.Environment.IsDevelopment());

await app.RunAsync();
