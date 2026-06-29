using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.Persistence.Configurations;

public class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> builder)
    {
        builder.ToTable("Workouts");
        
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.AthleteId)
            .IsRequired();
            
        builder.Property(w => w.WorkoutDayId)
            .IsRequired();
            
        builder.Property(w => w.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(w => w.CompletedAt)
            .IsRequired(false);
            
        // Assuming Workout has a collection of Exercise or similar in the future,
        // we'd map it here. For now it's just the basic fields.
    }
}
