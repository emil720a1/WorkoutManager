using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.Persistence.Configurations;

public class AthleteConfiguration : IEntityTypeConfiguration<Athlete>
{
    public void Configure(EntityTypeBuilder<Athlete> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TelegramId).IsRequired();
        builder.HasIndex(a => a.TelegramId).IsUnique();
        
        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        
        builder.Property(a => a.NotificationTime).IsRequired();
        
        builder.Property(a => a.CurrentProgramId).IsRequired(false);
    }
}
