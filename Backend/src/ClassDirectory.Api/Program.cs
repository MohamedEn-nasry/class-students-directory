using System.Threading.RateLimiting;
using ClassDirectory.Api.Data;
using ClassDirectory.Api.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog (console; config-driven) ----------
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// ---------- Config sections ----------
builder.Services.Configure<IpHashingOptions>(builder.Configuration.GetSection("IpHashing"));
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection("Turnstile"));

// ---------- Database: Supabase pooler via env ConnectionStrings__Supabase ----------
// Supavisor transaction pooler (port 6543) does NOT support prepared statements,
// so we disable auto-prepare + connection reset by default (Database:DisablePreparedStatements=true).
var supabaseConn = builder.Configuration.GetConnectionString("Supabase");
if (string.IsNullOrWhiteSpace(supabaseConn))
{
    // Fail fast with a clear message instead of a cryptic Npgsql error.
    // In Development without a DB, tests still run (they don't need the DB).
    Console.WriteLine("WARNING: ConnectionStrings:Supabase is not set. API will start but DB calls will fail until ConnectionStrings__Supabase is provided.");
    supabaseConn = "Host=localhost;Port=5432;Database=missing;Username=missing;Password=missing";
}

var disablePrepared = builder.Configuration.GetValue("Database:DisablePreparedStatements", true);
var dataSourceBuilder = new NpgsqlDataSourceBuilder(supabaseConn);
if (disablePrepared)
{
    dataSourceBuilder.ConnectionStringBuilder.MaxAutoPrepare = 0;
    dataSourceBuilder.ConnectionStringBuilder.NoResetOnClose = true;
}

var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(dataSource, npgsql =>
    {
        // Supabase Postgres 15; pinning avoids a version-lookup roundtrip on startup.
        npgsql.SetPostgresVersion(new Version(15, 0));
    });
});

// ---------- Health (DB liveness; no extra package needed) ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");

// ---------- CORS: frontend origin(s) only ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Configuration["FrontendOrigin"] is { } single && !string.IsNullOrWhiteSpace(single)
        ? new[] { single }
        : Array.Empty<string>());
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            // No origins configured: allow none explicitly (same-origin only).
            // Keep the policy registered so [EnableCors]/UseCors never throws.
            policy.WithOrigins("https://localhost");
        }

        policy.WithMethods("GET", "POST", "OPTIONS")
            .AllowAnyHeader();
    });
});

// ---------- Rate limiting: 5 POSTs / IP / hour ----------
var postsPerHour = builder.Configuration.GetValue("RateLimit:PostsPerHour", 5);
if (postsPerHour is < 1 or > 1000)
{
    postsPerHour = 5;
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        var retryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra)
            ? ra.TotalSeconds.ToString("F0")
            : "3600";
        ctx.HttpContext.Response.Headers.RetryAfter = retryAfter;
        await ctx.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Too many submissions.",
            Detail = $"Limit is {postsPerHour} profiles per hour per IP. Try again later.",
            Status = StatusCodes.Status429TooManyRequests,
        }, cancellationToken: token);
    };

    options.AddPolicy("post-profiles", httpContext =>
    {
        var ip = ResolveClientIp(httpContext) ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = postsPerHour,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
});

static string? ResolveClientIp(HttpContext ctx)
{
    var forwarded = ctx.Request.Headers["X-Forwarded-For"].ToString();
    if (!string.IsNullOrWhiteSpace(forwarded))
    {
        var first = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }
    }

    return ctx.Connection.RemoteIpAddress?.ToString();
}

// ---------- App services ----------
builder.Services.AddScoped<IIpHashService, IpHashService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ITurnstileService, TurnstileService>();
builder.Services.AddHttpClient("turnstile", c => c.Timeout = TimeSpan.FromSeconds(10));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Class Students Directory", Version = "v1" });
});

var app = builder.Build();

// ---------- Middleware ----------
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
// Behind Azure App Service / containers the proxy is not loopback — trust the chain
// for IP + scheme purposes (we never use it for auth; there is no login).
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

app.UseExceptionHandler(); // ProblemDetails for unhandled exceptions
app.UseStatusCodePages();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.SerializeAsV2 = true;
    });
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposed for integration-test factories.
public partial class Program
{
}
