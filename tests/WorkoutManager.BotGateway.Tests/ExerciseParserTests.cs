using System.Linq;
using FluentAssertions;
using WorkoutManager.Application.Services;
using Xunit;

namespace WorkoutManager.BotGateway.Tests;

public class ExerciseParserTests
{
    private readonly ExerciseParser _sut = new ExerciseParser();

    [Fact]
    public void Parse_ValidString_ShouldReturnParsedExercises()
    {
        // Arrange
        var input = "Squats 3x10\nBench Press 4*8";

        // Act
        var result = _sut.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        result[0].Name.Should().Be("Squats");
        result[0].Volume.Sets.Should().Be(3);
        result[0].Volume.Reps.Should().Be("10");

        result[1].Name.Should().Be("Bench Press");
        result[1].Volume.Sets.Should().Be(4);
        result[1].Volume.Reps.Should().Be("8");
    }

    [Theory]
    [InlineData("Deadlift   3 x 5")]
    [InlineData("  Deadlift 3x5  ")]
    [InlineData("Deadlift 3*5")]
    public void Parse_StringWithExtraSpacesOrDifferentSeparators_ShouldParseCorrectly(string input)
    {
        // Act
        var result = _sut.Parse(input);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Deadlift");
        result.First().Volume.Sets.Should().Be(3);
        result.First().Volume.Reps.Should().Be("5");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Just text without sets and reps")]
    [InlineData("Squats 3 x")] // Missing reps
    [InlineData("Squats x 10")] // Missing sets
    public void Parse_InvalidString_ShouldGracefullyIgnoreAndReturnEmpty(string input)
    {
        // Act
        var result = _sut.Parse(input);

        // Assert
        result.Should().BeEmpty();
    }
}
