using GymManagementSystem.Data;
using GymManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

if (args.Contains("--generate-admin-password-hash", StringComparer.Ordinal))
{
    return await AdminPasswordHashGenerator.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

var configuredDatabasePath = Environment.GetEnvironmentVariable("GYM_DATABASE_PATH");
var databasePath = string.IsNullOrWhiteSpace(configuredDatabasePath)
    ? builder.Environment.IsProduction()
        ? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "private", "gym.db"))
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "gym.db"))
    : Path.GetFullPath(Path.IsPathRooted(configuredDatabasePath)
        ? configuredDatabasePath
        : Path.Combine(builder.Environment.ContentRootPath, configuredDatabasePath));
var databaseDirectory = Path.GetDirectoryName(databasePath)
    ?? throw new InvalidOperationException("The configured SQLite database path has no parent directory.");
Directory.CreateDirectory(databaseDirectory);

var configuredConnectionString = builder.Configuration.GetConnectionString("GymDatabase")
    ?? throw new InvalidOperationException("The GymDatabase connection string is missing.");
var connectionString = new SqliteConnectionStringBuilder(configuredConnectionString)
{
    DataSource = databasePath,
    ForeignKeys = true
}.ToString();

if (builder.Environment.IsProduction())
{
    var keyDirectory = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, "..", "private", "keys"));
    Directory.CreateDirectory(keyDirectory);
    builder.Services.AddDataProtection()
        .SetApplicationName("MuscleMemory")
        .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddRazorPages();
builder.Services.AddSingleton<PasswordHasher<AdminCredential>>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "GymManagement.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Too many login attempts. Please wait a minute and try again.",
            cancellationToken);
    };
});

var app = builder.Build();
var schemaScriptPath = Path.Combine(app.Environment.ContentRootPath, "Database.sql");
if (!File.Exists(schemaScriptPath))
{
    schemaScriptPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "Database"));
}

await DatabaseInitializer.InitializeAsync(
    connectionString,
    schemaScriptPath);

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseWhen(
    context => string.Equals(context.Request.Path.Value?.TrimEnd('/'), "/Account/Login", StringComparison.OrdinalIgnoreCase) &&
               HttpMethods.IsPost(context.Request.Method),
    loginPipeline => loginPipeline.UseRateLimiter());
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets()
    .AllowAnonymous();
app.MapRazorPages()
    .WithStaticAssets();

await app.RunAsync();
return 0;
