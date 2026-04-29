namespace WorkoutManager.Application.Common.Options;

public class BotConfiguration
{
    public const string Configuration = "BotConfiguration";
    
    public string BotToken { get; init; } = default!;
    public long AdminTelegramId { get; init; }
}
