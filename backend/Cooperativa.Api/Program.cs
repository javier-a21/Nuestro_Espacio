using System.Text;
using Cooperativa.Api.Auth;
using Cooperativa.Api.Jobs;
using Cooperativa.Api.Realtime;
using Cooperativa.Api.Services;
using Cooperativa.Domain.Entities;
using Cooperativa.Infrastructure;
using Cooperativa.Infrastructure.Persistence;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Cadena de conexión ---
// Si hay Postgres (Neon) lo usamos; si no, caemos a SQLite local para desarrollar sin dependencias.
var pgConn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(pgConn))
    pgConn = Environment.GetEnvironmentVariable("COOPERATIVA_DB");

var useSqlite = string.IsNullOrWhiteSpace(pgConn);
var connectionString = useSqlite ? "Data Source=cooperativa-dev.db" : pgConn!;

// --- EF Core (PostgreSQL en real, SQLite en dev) ---
builder.Services.AddInfrastructure(connectionString, useSqlite);

// --- Identity (solo núcleo: API con JWT, sin cookies) ---
builder.Services
    .AddIdentityCore<AppUser>(opt =>
    {
        opt.Password.RequiredLength = 6;
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

// --- JWT ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true
        };

        // WebSockets no envían cabeceras: aceptamos el token por query para el Hub.
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs/cooperative"))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// --- SignalR + presencia + notificador ---
builder.Services.AddSignalR();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddScoped<IRoomNotifier, HubRoomNotifier>();

// --- Servicios de aplicación ---
builder.Services.AddScoped<CooperativeService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<RoomTickJob>();

// --- Hangfire (Postgres en real; almacenamiento en memoria en dev con SQLite) ---
builder.Services.AddHangfire(cfg =>
{
    cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
       .UseSimpleAssemblyNameTypeSerializer()
       .UseRecommendedSerializerSettings();
    if (useSqlite) cfg.UseMemoryStorage();
    else cfg.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString));
});
builder.Services.AddHangfireServer();

// --- CORS para el cliente Vite ---
const string ClientCors = "client";
builder.Services.AddCors(o => o.AddPolicy(ClientCors, p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Crea (SQLite dev) o migra (Postgres) la base de datos al arrancar.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (useSqlite) db.Database.EnsureCreated();
    else db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(ClientCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CooperativeHub>("/hubs/cooperative");
app.MapHangfireDashboard("/hangfire"); // TODO: proteger en producción

// Job recurrente: tick horario que cierra rachas, decadencia, clima y cambio de huso.
RecurringJob.AddOrUpdate<RoomTickJob>("room-tick", j => j.RunAsync(), Cron.Hourly());

app.Run();
