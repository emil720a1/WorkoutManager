namespace WorkoutManager.Application.Services;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WorkoutManager.Domain.Entities;

public class ExerciseParser
{
    public List<Exercise> Parse(string text)
    {
        var exercises = new List<Exercise>();
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

                var exercise = Exercise.CreateParsed(name, sets, reps);

                exercises.Add(exercise);
            }
        }

        return exercises;
    }
}
