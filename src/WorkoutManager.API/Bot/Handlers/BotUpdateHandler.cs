using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WorkoutManager.Application.Common.Options;
using WorkoutManager.Application.Enums;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Application.DTOs;
using WorkoutManager.Application.Services;

namespace WorkoutManager.API.Bot.Handlers;

public class BotUpdateHandler(
    ILogger<BotUpdateHandler> logger,
    ITelegramBotClient botClient,
    IOptions<BotConfiguration> options,
    IStateService stateService,
    Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory) : IBotUpdateHandler
{
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message is null)
        {
            return;
        }

        var message = update.Message;
        var chatId = message.Chat.Id;
        var userId = message.From?.Id ?? 0;
        var text = message.Text;
        var adminId = options.Value.AdminTelegramId;

        // if (userId != adminId)
        // {
        //     await botClient.SendMessage(chatId, "Access denied.", cancellationToken: cancellationToken);
        //     return;
        // }

        if (text == "/cancel")
        {
            await stateService.ClearStateAsync(userId);
            await botClient.SendMessage(chatId, "Dialog cancelled.", cancellationToken: cancellationToken);
            return;
        }

        var currentState = await stateService.GetStateAsync(userId);

        if (string.IsNullOrWhiteSpace(text) && currentState != AdminDialogState.None)
        {
            await botClient.SendMessage(chatId, "Please send a text message for this step.", cancellationToken: cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            switch (currentState)
            {
                case AdminDialogState.None:
                    if (text == "/set_program")
                    {
                        await stateService.SetStateAsync(userId, AdminDialogState.WaitingForAthleteTelegramId);
                        await botClient.SendMessage(chatId, "Enter Athlete Telegram ID:", cancellationToken: cancellationToken);
                    }
                    else if (text == "/start")
                    {
                        await botClient.SendMessage(chatId, "Привіт! Я твій Workout-бот. Обери дію в меню.", cancellationToken: cancellationToken);
                    }
                    else if (text == "/help")
                    {
                        await botClient.SendMessage(chatId, "Цей бот допомагає керувати тренуваннями. Використовуй меню зліва для навігації.", cancellationToken: cancellationToken);
                    }
                    break;

                case AdminDialogState.WaitingForAthleteTelegramId:
                    if (long.TryParse(text, out var athleteId))
                    {
                        var draft = new WorkoutDraft { AthleteId = athleteId };
                        await stateService.SetDraftAsync(userId, draft);
                        await stateService.SetStateAsync(userId, AdminDialogState.WaitingForDayOfWeek);
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
                        var draft = await stateService.GetDraftAsync(userId);
                        if (draft != null)
                        {
                            draft.DayOfWeek = day;
                            await stateService.SetDraftAsync(userId, draft);
                            await stateService.SetStateAsync(userId, AdminDialogState.WaitingForExercisesList);
                            await botClient.SendMessage(chatId, "Enter exercises list (e.g., Squats 3x10):", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Invalid day. Enter a number from 1 to 7.", cancellationToken: cancellationToken);
                    }
                    break;

                case AdminDialogState.WaitingForExercisesList:
                    var finalDraft = await stateService.GetDraftAsync(userId);
                    if (finalDraft != null)
                    {
                        finalDraft.ExercisesText = text;
                        var parser = new ExerciseParser();
                        var exercises = parser.Parse(text);
                        
                        await using var scope = scopeFactory.CreateAsyncScope();
                        var _workoutService = scope.ServiceProvider.GetRequiredService<IWorkoutService>();

                        var saveResult = await _workoutService.SaveParsedWorkoutAsync(finalDraft.AthleteId, finalDraft.DayOfWeek, exercises, cancellationToken);
                        
                        if (saveResult.IsSuccess)
                        {
                            await botClient.SendMessage(chatId, "Program received and saved successfully!", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, $"Error: {saveResult.Error}", cancellationToken: cancellationToken);
                        }

                        await stateService.ClearStateAsync(userId);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing update for user {UserId}", userId);
            await stateService.ClearStateAsync(userId);
            await botClient.SendMessage(chatId, "Server error. Flow was reset.", cancellationToken: cancellationToken);
        }
    }

    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram error");
        return Task.CompletedTask;
    }
}
