using AutoBot.Models;
using AutoBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure appsettings files manually to ensure environment-specific settings are loaded
var contentRoot = builder.Environment.ContentRootPath;
builder.Configuration
    .AddJsonFile(Path.Combine(contentRoot, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddJsonFile(Path.Combine(contentRoot, $"appsettings.Development.json"), optional: true, reloadOnChange: true);

// Configure Options pattern
builder.Services.AddOptions<LnMarketsOptions>()
    .Bind(builder.Configuration.GetSection(LnMarketsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add Web API services
builder.Services.AddControllers();

// Add HttpClient and logging
builder.Services.AddHttpClient();
builder.Services.AddLogging();

builder.Services.AddSingleton<IMarketplaceClient, LnMarketsClient>();
builder.Services.AddSingleton<ITradeManager, TradeManager>();
builder.Services.AddSingleton<IPriceQueue, PriceQueue>();
builder.Services.AddHostedService<LnMarketsBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
