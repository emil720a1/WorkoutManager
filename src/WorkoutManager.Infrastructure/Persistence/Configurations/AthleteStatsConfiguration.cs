using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.Persistence.Configurations;

public class AthleteStatsConfiguration : IEntityTypeConfiguration<AthleteStats>
{
    public void Configure(EntityTypeBuilder<AthleteStats> builder)
    {
        builder.ToTable("AthleteStats");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.AthleteId)
            .IsRequired();
            
        builder.Property(s => s.CurrentStreak)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(s => s.HighestStreak)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(s => s.TotalTonnage)
            .IsRequired()
            .HasDefaultValue(0.0);
    }
}
