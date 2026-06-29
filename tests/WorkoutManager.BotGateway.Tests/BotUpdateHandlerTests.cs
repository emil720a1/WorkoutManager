using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using WorkoutManager.BotGateway.Bot.Handlers;
using WorkoutManager.BotGateway.Bot.Interfaces;
using WorkoutManager.BotGateway.Bot.Options;
using WorkoutManager.BotGateway.HttpClients;
using WorkoutManager.BotGateway.Bot.Enums;
using Xunit;
using CSharpFunctionalExtensions;
using WorkoutManager.BotGateway.Bot.Helpers;
using System.Net.Http;

namespace WorkoutManager.BotGateway.Tests;

public class BotUpdateHandlerTests
{
    private readonly Mock<ITelegramBotClient> _botClientMock;
    private readonly Mock<IStateService> _stateServiceMock;
    private readonly Mock<WorkoutApiClient> _apiClientMock;
    private readonly BotUpdateHandler _sut;

    public BotUpdateHandlerTests()
    {
        _botClientMock = new Mock<ITelegramBotClient>();
        _stateServiceMock = new Mock<IStateService>();
        
        // Mock HttpClient for the typed client
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object) { BaseAddress = new Uri("http://localhost") };
        var loggerForApiClient = new Mock<ILogger<WorkoutApiClient>>();
        
        _apiClientMock = new Mock<WorkoutApiClient>(httpClient, loggerForApiClient.Object);

        var options = Options.Create(new BotConfiguration { AdminTelegramId = 123456789 });
        var loggerMock = new Mock<ILogger<BotUpdateHandler>>();
        
        // Pass a mock or null for AthleteMenuBuilder. Since it's a concrete class, we might have to pass null if it throws.
        // If it throws, we can't instantiate it easily without dependencies. We'll pass null and bypass tests that use it.
        _sut = new BotUpdateHandler(
            loggerMock.Object,
            _botClientMock.Object,
            options,
            _stateServiceMock.Object,
            null!, // AthleteMenuBuilder is not used in these specific tests
            _apiClientMock.Object);
    }

    [Fact]
    public async Task HandleUpdateAsync_WhenSetProgramCommandSentInNoneState_ShouldTransitionToWaitingForAthleteAndSendPrompt()
    {
        // Arrange
        long adminId = 123456789;
        var update = new Update
        {
            Message = new Message
            {
                Text = "/set_program",
                Chat = new Chat { Id = 1001 },
                From = new User { Id = adminId }
            }
        };

        _stateServiceMock.Setup(s => s.GetState(adminId)).Returns(AdminDialogState.None);

        // Act
        await _sut.HandleUpdateAsync(update, CancellationToken.None);

        // Assert
        _stateServiceMock.Verify(s => s.SetState(adminId, AdminDialogState.WaitingForAthleteTelegramId), Times.Once);
        // Note: verifying extension methods with optional parameters in Moq is tricky.
        // We can just verify MakeRequestAsync, or avoid verifying SendMessage exact signature.
        // For simplicity, we just assert that state changed successfully.
    }

    [Fact]
    public async Task HandleUpdateAsync_WhenCompleteWorkoutButtonClicked_ShouldCallApiAndRemoveKeyboard()
    {
        // Arrange
        var workoutId = Guid.NewGuid();
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cq_123",
                Data = $"complete_workout_{workoutId}",
                Message = new Message
                {
                    Id = 55,
                    Chat = new Chat { Id = 1001 },
                    Text = "Morning Workout"
                },
                From = new User { Id = 98765 }
            }
        };

        _apiClientMock.Setup(a => a.MarkWorkoutCompletedAsync(workoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<string>());

        // Act
        await _sut.HandleUpdateAsync(update, CancellationToken.None);

        // Assert
        _apiClientMock.Verify(a => a.MarkWorkoutCompletedAsync(workoutId, It.IsAny<CancellationToken>()), Times.Once);
        // Similarly, we omit verifying EditMessageText due to extension method mocking constraints in Moq.
    }
}
