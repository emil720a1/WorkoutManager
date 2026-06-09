using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Telegram.Bot;

using WorkoutManager.BotGateway.Bot.Handlers;
using WorkoutManager.BotGateway.Bot.Interfaces;
using WorkoutManager.BotGateway.Bot.Options;
using WorkoutManager.BotGateway.Bot.Services;
using WorkoutManager.BotGateway.Bot.Workers;

var builder = WebApplication.CreateBuilder(args);

// Зчитування конфігурації бота
builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection(BotConfiguration.Configuration));

// Реєстрація Redis
string redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));

// Налаштування клієнта Telegram
builder.Services.AddHttpClient("tgclient")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        var botConfig = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
        return new TelegramBotClient(botConfig.BotToken, httpClient);
    });

// Реєстрація Redis State сесій
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});
builder.Services.AddSingleton<IStateService, RedisStateService>();

// Реєстрація хандлерів та воркерів
builder.Services.AddSingleton<IBotUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<TelegramBotBackgroundService>();
builder.Services.AddHostedService<NotificationSubscriberWorker>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();
