using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.TelegramId);

        builder.Property(u => u.TelegramId)
            .HasColumnName("telegram_id")
            .ValueGeneratedNever();

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100);

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(100);

        builder.Property(u => u.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(10);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(u => u.LastSeenAt)
            .HasColumnName("last_seen_at")
            .IsRequired();

        // Navigation relationships defined in other configurations
    }
}
