using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WorkoutManager.BotGateway.Bot.DTOs;
using WorkoutManager.BotGateway.Bot.Enums;
using WorkoutManager.BotGateway.Bot.Interfaces;
using WorkoutManager.BotGateway.Bot.Options;
using WorkoutManager.BotGateway.Common;
using WorkoutManager.Contracts;

namespace WorkoutManager.BotGateway.Bot.Handlers;

public class BotUpdateHandler(
    ILogger<BotUpdateHandler> logger,
    ITelegramBotClient botClient,
    IOptions<BotConfiguration> options,
    IStateService stateService,
    IConnectionMultiplexer redisMultiplexer) : IBotUpdateHandler
{
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null)
        {
            return;
        }

        var message = update.Message;
        var chatId = message.Chat.Id;
        var text = message.Text;
        var adminId = options.Value.AdminTelegramId;

        if (message.From?.Id != adminId)
        {
            return;
        }

        if (text == "/cancel")
        {
            stateService.ClearState(adminId);
            await botClient.SendMessage(chatId, "Dialog cancelled.", cancellationToken: cancellationToken);
            return;
        }

        var currentState = stateService.GetState(adminId);

        try
        {
            switch (currentState)
            {
                case AdminDialogState.None:
                    if (text == "/start" || text == "/help")
                    {
                        await botClient.SendMessage(chatId,
                            "👋 Привіт, адміне!\n\nДоступні команди:\n/set_program — задати програму тренувань\n/cancel — скасувати поточний діалог",
                            cancellationToken: cancellationToken);
                    }
                    else if (text == "/set_program")
                    {
                        stateService.SetState(adminId, AdminDialogState.WaitingForAthleteTelegramId);
                        await botClient.SendMessage(chatId, "Enter Athlete Telegram ID:", cancellationToken: cancellationToken);
                    }
                    break;

                case AdminDialogState.WaitingForAthleteTelegramId:
                    if (long.TryParse(text, out var athleteId))
                    {
                        var draft = new WorkoutDraft { AthleteId = athleteId };
                        stateService.SetDraft(adminId, draft);
                        stateService.SetState(adminId, AdminDialogState.WaitingForDayOfWeek);
                        await botClient.SendMessage(chatId, "Enter Day of Week (1 for Monday, 7 for Sunday):", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Invalid ID format. Please enter numbers only.", cancellationToken: cancellationToken);
                    }
                    break;

                case AdminDialogState.WaitingForDayOfWeek:
                    if (int.TryParse(text, out var day) && day is >= 1 and <= 7)
                    {
                        var draft = stateService.GetDraft(adminId);
                        if (draft != null)
                        {
                            draft.DayOfWeek = day;
                            stateService.SetDraft(adminId, draft);
                            stateService.SetState(adminId, AdminDialogState.WaitingForExercisesList);
                            await botClient.SendMessage(chatId, "Enter exercises list (e.g., Squats 3x10):", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Invalid day. Enter a number from 1 to 7.", cancellationToken: cancellationToken);
                    }
                    break;

                case AdminDialogState.WaitingForExercisesList:
                    var finalDraft = stateService.GetDraft(adminId);
                    if (finalDraft != null)
                    {
                        finalDraft.ExercisesText = text;
                        var parser = new ExerciseParser();

                        try
                        {
                            var exerciseDtos = parser.Parse(text);

                            var integrationEvent = new WorkoutCreatedEvent
                            {
                                AthleteId = finalDraft.AthleteId,
                                DayOfWeek = finalDraft.DayOfWeek,
                                Exercises = exerciseDtos
                            };

                            var jsonPayload = JsonSerializer.Serialize(integrationEvent);
                            var subscriber = redisMultiplexer.GetSubscriber();

                            logger.LogInformation("Publishing WorkoutCreatedEvent {EventId} to Redis for user {UserId}", integrationEvent.EventId, adminId);

                            await subscriber.PublishAsync(
                                RedisChannel.Literal("workout_channel"),
                                new RedisValue(jsonPayload)
                            );

                            await botClient.SendMessage(
                                chatId,
                                "Вашу програму тренувань прийнято та надіслано на обробку сервером!",
                                cancellationToken: cancellationToken
                            );
                        }
                        catch (RedisException redisEx)
                        {
                            logger.LogError(redisEx, "Redis transport failure while publishing workout for user {UserId}", adminId);
                            await botClient.SendMessage(
                                chatId,
                                "Вибачте, сталася помилка зв'язку з брокером повідомлень (Redis). Ми спробуємо обробити її пізніше.",
                                cancellationToken: cancellationToken
                            );
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Unexpected error compiling exercises draft for user {UserId}", adminId);
                            await botClient.SendMessage(
                                chatId,
                                "Сталася критична помилка під час обробки вашої програми.",
                                cancellationToken: cancellationToken
                            );
                        }
                        finally
                        {
                            stateService.ClearState(adminId);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing update");
            await botClient.SendMessage(chatId, "Server error.", cancellationToken: cancellationToken);
        }
    }

    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram error");
        return Task.CompletedTask;
    }
}
