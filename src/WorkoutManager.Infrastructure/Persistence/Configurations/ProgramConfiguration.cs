using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.Persistence.Configurations;

public class ProgramConfiguration : IEntityTypeConfiguration<Program>
{
    public void Configure(EntityTypeBuilder<Program> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);

        var navigation = builder.Metadata.FindNavigation(nameof(Program.Days));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Days)
            .WithOne()
            .HasForeignKey("ProgramId") // Shadow property (неявне поле зовнішнього ключа)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
