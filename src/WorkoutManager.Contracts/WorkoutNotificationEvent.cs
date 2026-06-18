using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorkoutManager.Contracts;

public sealed record WorkoutNotificationEvent(
    long AthleteTelegramId,
    string AthleteName,
    IReadOnlyList<ExerciseNotificationDto> Exercises)
{
    [JsonIgnore] 
    public string ChannelName => "workout:notifications";
}

public sealed record ExerciseNotificationDto(
    string Name,
    int Sets,
    string Reps);
