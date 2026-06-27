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
using WorkoutManager.BotGateway.Bot.Helpers;

namespace WorkoutManager.BotGateway.Bot.Handlers;

public class BotUpdateHandler(
    ILogger<BotUpdateHandler> logger,
    ITelegramBotClient botClient,
    IOptions<BotConfiguration> options,
    IStateService stateService,
    AthleteMenuBuilder athleteMenuBuilder,
    WorkoutManager.BotGateway.HttpClients.WorkoutApiClient apiClient) : IBotUpdateHandler
{
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
            return;
        }

        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            await HandleMessageAsync(update.Message, cancellationToken);
            return;
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
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
                            "👋 Привіт, адміне!\n\nДоступні команди:\n/my_athletes — view active athletes\n/set_program — задати програму тренувань\n/cancel — скасувати поточний діалог",
                            cancellationToken: cancellationToken);
                    }
                    else if (text == "/my_athletes")
                    {
                        var keyboard = await athleteMenuBuilder.BuildStudentListAsync(adminId, cancellationToken);
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "📋 Your Active Athletes:",
                            replyMarkup: keyboard,
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
                        stateService.SetState(adminId, AdminDialogState.AwaitingWorkoutInput);
                        await botClient.SendMessage(chatId, "🎯 Athlete selected! Now, please enter the workout text (e.g. Squats 3x10).", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "❌ Invalid ID format. Please enter numbers only.", cancellationToken: cancellationToken);
                    }
                    break;

                case AdminDialogState.AwaitingWorkoutInput:
                    var finalDraft = stateService.GetDraft(adminId);
                    if (finalDraft != null)
                    {
                        finalDraft.ExercisesText = text;
                        var parser = new ExerciseParser();

                        try
                        {
                            var exerciseDtos = parser.Parse(text);

                            if (exerciseDtos.Count == 0)
                            {
                                await botClient.SendMessage(
                                    chatId,
                                    "❌ Invalid format. Please use: [Exercise Name] [Sets]x[Reps]",
                                    cancellationToken: cancellationToken
                                );
                                // Do not clear state, allow retry
                                break;
                            }

                            var result = await apiClient.AssignWorkoutAsync(finalDraft.AthleteId, exerciseDtos, cancellationToken);

                            if (result.IsSuccess)
                            {
                                await botClient.SendMessage(
                                    chatId,
                                    "💪 Workout successfully assigned!",
                                    cancellationToken: cancellationToken
                                );
                                stateService.ClearState(adminId);
                            }
                            else
                            {
                                logger.LogError("Failed to assign workout for user {UserId}: {Error}", adminId, result.Error);
                                await botClient.SendMessage(
                                    chatId,
                                    $"⚠️ Could not assign workout: {result.Error}",
                                    cancellationToken: cancellationToken
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Unexpected error compiling exercises draft for user {UserId}", adminId);
                            await botClient.SendMessage(
                                chatId,
                                "❌ A critical error occurred while processing your workout program.",
                                cancellationToken: cancellationToken
                            );
                            stateService.ClearState(adminId);
                        }
                    }
                    else
                    {
                        stateService.ClearState(adminId);
                        await botClient.SendMessage(chatId, "❌ Dialog session expired. Please start over.", cancellationToken: cancellationToken);
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

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var adminId = options.Value.AdminTelegramId;

        if (callbackQuery.Data?.StartsWith("select_student:") == true)
        {
            var idPart = callbackQuery.Data.Substring("select_student:".Length);

            if (callbackQuery.From.Id != adminId)
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Access denied.", cancellationToken: cancellationToken);
                return;
            }

            if (long.TryParse(idPart, out var athleteTelegramId))
            {
                var currentState = stateService.GetState(adminId);
                
                if (currentState == AdminDialogState.None)
                {
                    var draft = new WorkoutDraft { AthleteId = athleteTelegramId };
                    stateService.SetDraft(adminId, draft);
                    
                    stateService.SetState(adminId, AdminDialogState.AwaitingWorkoutInput);

                    await botClient.AnswerCallbackQuery(
                        callbackQueryId: callbackQuery.Id,
                        text: "Athlete Selected!",
                        cancellationToken: cancellationToken);

                    await botClient.SendMessage(
                        chatId: callbackQuery.Message?.Chat.Id ?? adminId,
                        text: "🎯 Athlete selected! Now, please enter the workout text (e.g. Squats 3x10).",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQueryId: callbackQuery.Id,
                        text: "You are already in a dialog. Cancel it first.",
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid callback data.",
                    cancellationToken: cancellationToken);
            }
        }
    }

    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram error");
        return Task.CompletedTask;
    }
}
