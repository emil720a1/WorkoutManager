using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.Persistence.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);

        builder.ComplexProperty(e => e.Volume, cb =>
        {
            cb.Property(v => v.Sets)
                .HasColumnName("Volume_Sets")
                .IsRequired();
            
            cb.Property(v => v.Reps)
                .HasColumnName("Volume_Reps")
                .IsRequired()
                .HasMaxLength(50);
        });
    }
}
