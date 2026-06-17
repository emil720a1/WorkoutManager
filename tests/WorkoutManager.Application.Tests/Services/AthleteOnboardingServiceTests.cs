using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Moq;
using WorkoutManager.Application.Errors;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Application.Services;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Domain.Interfaces;
using Xunit;

namespace WorkoutManager.Application.Tests.Services;

public class AthleteOnboardingServiceTests
{
    private readonly Mock<IAthleteRepository>                _athleteRepoMock = new();
    private readonly Mock<IAthleteRedisRepository>           _redisRepoMock   = new();
    private readonly Mock<IApplicationDbContext>             _dbContextMock   = new();
    private readonly Mock<ILogger<AthleteOnboardingService>> _loggerMock      = new();

    private AthleteOnboardingService CreateSut() => new(
        _athleteRepoMock.Object,
        _redisRepoMock.Object,
        _dbContextMock.Object,
        _loggerMock.Object);

    // =======================================================================
    // RegisterPendingAthleteAsync
    // =======================================================================

    [Fact]
    public async Task RegisterPendingAthleteAsync_Should_SaveToDbAndRedis_When_AthleteIsNew()
    {
        // Arrange
        const long   coachId     = 927118456L;
        const string rawUsername = "@Alex_PowerLifter";   // un-normalized input
        const string displayName = "Alex";
        const string normalized  = "alex_powerlifter";   // expected after trimming + lowercase

        _athleteRepoMock
            .Setup(r => r.GetPendingByUsernameAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Athlete?)null);

        _dbContextMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _redisRepoMock
            .Setup(r => r.AddPendingAthleteAsync(coachId, normalized, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().RegisterPendingAthleteAsync(coachId, rawUsername, displayName);

        // Assert — Result is Success
        Assert.True(result.IsSuccess);

        _athleteRepoMock.Verify(r =>
            r.AddAsync(
                It.Is<Athlete>(a =>
                    a.Username    == normalized        &&
                    a.TelegramId  == 0                 &&
                    a.Status      == AthleteStatus.Pending &&
                    a.Name        == displayName),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _redisRepoMock.Verify(r =>
            r.AddPendingAthleteAsync(coachId, normalized, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterPendingAthleteAsync_Should_ReturnAthleteAlreadyExists_When_DuplicateRegistration()
    {
        // Arrange
        const long   coachId     = 927118456L;
        const string rawUsername = "@alex_powerlifter";
        const string normalized  = "alex_powerlifter";

        var existingAthlete = new Athlete(telegramId: 0, name: "Alex", username: normalized);

        _athleteRepoMock
            .Setup(r => r.GetPendingByUsernameAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAthlete);

        // Act
        var result = await CreateSut().RegisterPendingAthleteAsync(coachId, rawUsername, "Alex");

        // Assert — Result is Failure with correct typed error
        Assert.True(result.IsFailure);
        Assert.IsType<OnboardingError.AthleteAlreadyExists>(result.Error);
        Assert.Equal(normalized, ((OnboardingError.AthleteAlreadyExists)result.Error).Username);

        // No DB or Redis writes must occur on duplicate
        _athleteRepoMock.Verify(r =>
            r.AddAsync(It.IsAny<Athlete>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _dbContextMock.Verify(db =>
            db.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _redisRepoMock.Verify(r =>
            r.AddPendingAthleteAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =======================================================================
    // TryBindAthleteAsync
    // =======================================================================

    [Fact]
    public async Task TryBindAthleteAsync_Should_UpdateDbAndCacheProfile_When_AthleteIsPending()
    {
        // Arrange
        const long   coachId          = 927118456L;
        const long   athleteTelegramId = 123456789L;
        const string rawUsername       = "@alex_powerlifter";
        const string normalized        = "alex_powerlifter";

        var pendingAthlete = new Athlete(telegramId: 0, name: "Alex", username: normalized);

        _athleteRepoMock
            .Setup(r => r.GetPendingByUsernameAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingAthlete);

        _dbContextMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _redisRepoMock
            .Setup(r => r.BindAthleteAsync(coachId, normalized, athleteTelegramId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _redisRepoMock
            .Setup(r => r.CacheAthleteProfileAsync(athleteTelegramId, "Alex", normalized, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().TryBindAthleteAsync(coachId, athleteTelegramId, rawUsername);

        // Assert — Result is Success, domain entity mutated correctly
        Assert.True(result.IsSuccess);
        Assert.Equal(athleteTelegramId, pendingAthlete.TelegramId);
        Assert.Equal(AthleteStatus.Active, pendingAthlete.Status);

        _athleteRepoMock.Verify(r =>
            r.Update(It.Is<Athlete>(a => a.TelegramId == athleteTelegramId)),
            Times.Once);

        _dbContextMock.Verify(db =>
            db.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _redisRepoMock.Verify(r =>
            r.BindAthleteAsync(coachId, normalized, athleteTelegramId, It.IsAny<CancellationToken>()),
            Times.Once);

        _redisRepoMock.Verify(r =>
            r.CacheAthleteProfileAsync(athleteTelegramId, "Alex", normalized, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryBindAthleteAsync_Should_ReturnAthleteNotFound_When_NoPendingAthleteIsFound()
    {
        // Arrange
        const long   coachId          = 927118456L;
        const long   athleteTelegramId = 999999999L;
        const string rawUsername       = "@unknown_user";
        const string normalized        = "unknown_user";

        _athleteRepoMock
            .Setup(r => r.GetPendingByUsernameAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Athlete?)null);

        // Act
        var result = await CreateSut().TryBindAthleteAsync(coachId, athleteTelegramId, rawUsername);

        // Assert — typed AthleteNotFound error, nothing mutated
        Assert.True(result.IsFailure);
        Assert.IsType<OnboardingError.AthleteNotFound>(result.Error);
        Assert.Equal(normalized, ((OnboardingError.AthleteNotFound)result.Error).Username);

        _athleteRepoMock.Verify(r => r.Update(It.IsAny<Athlete>()), Times.Never);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _redisRepoMock.Verify(r =>
            r.BindAthleteAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _redisRepoMock.Verify(r =>
            r.CacheAthleteProfileAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryBindAthleteAsync_Should_ReturnDomainValidationError_When_AthleteIsAlreadyActive()
    {
        // Arrange
        const long   coachId          = 927118456L;
        const long   athleteTelegramId = 123456789L;
        const string rawUsername       = "@alex_powerlifter";
        const string normalized        = "alex_powerlifter";

        // Athlete is already Active (was previously bound)
        var activeAthlete = new Athlete(telegramId: athleteTelegramId, name: "Alex", username: normalized);

        _athleteRepoMock
            .Setup(r => r.GetPendingByUsernameAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeAthlete);

        // Act
        var result = await CreateSut().TryBindAthleteAsync(coachId, athleteTelegramId, rawUsername);

        // Assert — DomainValidationError surfaced, no side effects
        Assert.True(result.IsFailure);
        Assert.IsType<OnboardingError.DomainValidationError>(result.Error);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _redisRepoMock.Verify(r =>
            r.BindAthleteAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
