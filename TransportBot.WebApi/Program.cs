using Microsoft.EntityFrameworkCore;
using TransportBot.Infrastructure.Data;
using TransportBot.Core.Interfaces;
using TransportBot.Infrastructure.Repositories;
using TransportBot.Services;
using TransportBot.Services.Services;
using TransportBot.Services.BackgroundServices;
using Telegram.Bot;
using TransportBot.WebApi.Middleware;
using TransportBot.WebApi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var token = configuration["TelegramBot:Token"];
    return new TelegramBotClient(token);
});

builder.Services.AddScoped<ITelegramBotService, TelegramBotService>();
builder.Services.AddScoped<ITransportStopRepository, TransportStopRepository>();
builder.Services.AddScoped<ITransportStopService, TransportStopService>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.Configure<TransportBot.Core.Configuration.YandexApiConfiguration>(
    builder.Configuration.GetSection("YandexApi"));
builder.Services.Configure<TransportBot.Core.Configuration.GeocodingConfiguration>(
    builder.Configuration.GetSection("Geocoding"));
builder.Services.Configure<TransportBot.Core.Configuration.MosgorpassConfiguration>(
    builder.Configuration.GetSection("Mosgorpass"));

builder.Services.AddHttpClient<IYandexApiService, YandexApiService>();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();
builder.Services.AddScoped<ITransportApiService, RefactoredTransportApiService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddSimpleRateLimiting();
builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services.AddHostedService<TelegramBotPollingService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Transport Notification Bot API", 
        Version = "v1",
        Description = "API для управления транспортными уведомлениями"
    });
    c.CustomSchemaIds(type => type.FullName);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TransportDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TransportDbContext>();
    await context.Database.MigrateAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Error migrating database");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSimpleRateLimiting();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application is shutting down...");
    Log.CloseAndFlush();
});

app.Run();
