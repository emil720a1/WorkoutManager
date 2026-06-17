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
        // TelegramId=0 is valid for pending athletes, so we cannot have a global unique index
        // We use a filtered unique index for active athletes only
        builder.HasIndex(a => a.TelegramId)
            .IsUnique()
            .HasFilter("\"TelegramId\" != 0");

        builder.Property(a => a.Username)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);
        builder.HasIndex(a => a.Username);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(AthleteStatus.Pending);

        builder.Property(a => a.NotificationTime).IsRequired();

        builder.Property(a => a.CurrentProgramId).IsRequired(false);
    }
}
