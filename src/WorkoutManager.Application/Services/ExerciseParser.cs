namespace WorkoutManager.Application.Services;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Domain.ValueObjects;

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

                var volume = new WorkoutVolume(sets, reps);
                var exercise = (Exercise)Activator.CreateInstance(
                    typeof(Exercise), 
                    BindingFlags.NonPublic | BindingFlags.Instance, 
                    null, 
                    [name, volume], 
                    null)!;

                exercises.Add(exercise);
            }
        }

        return exercises;
    }
}
