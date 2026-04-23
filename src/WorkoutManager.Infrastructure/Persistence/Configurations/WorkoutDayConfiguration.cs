using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.Persistence.Configurations;

public class WorkoutDayConfiguration : IEntityTypeConfiguration<WorkoutDay>
{
    public void Configure(EntityTypeBuilder<WorkoutDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DayNumber).IsRequired();
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);

        var navigation = builder.Metadata.FindNavigation(nameof(WorkoutDay.Exercises));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(d => d.Exercises)
            .WithOne()
            .HasForeignKey("WorkoutDayId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
