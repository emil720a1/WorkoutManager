using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WorkoutManager.Contracts;

namespace WorkoutManager.BotGateway.Common;

public class ExerciseParser
{
    public List<ExerciseDto> Parse(string text)
    {
        var exercises = new List<ExerciseDto>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var regex = new Regex(@"^(.*?)\s+(\d+)[x*](\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                var name = match.Groups[1].Value.Trim();
                var sets = int.Parse(match.Groups[2].Value);
                var reps = match.Groups[3].Value;

                exercises.Add(new ExerciseDto
                {
                    Name = name,
                    Sets = sets,
                    Reps = reps
                });
            }
        }

        return exercises;
    }
}
