using WorkoutManager.Infrastructure;
using WorkoutManager.Application;
using WorkoutManager.API.Bot.Configuration;
using WorkoutManager.API.Bot.Handlers;
using WorkoutManager.API.Bot.Services;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection(BotConfiguration.Configuration));
builder.Services.AddHttpClient("tgclient")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        var botConfig = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BotConfiguration>>().Value;
        return new TelegramBotClient(botConfig.BotToken, httpClient);
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton<IBotUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<TelegramBotBackgroundService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
