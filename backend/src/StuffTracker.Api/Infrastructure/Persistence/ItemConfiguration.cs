using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items", t =>
        {
            t.HasCheckConstraint("positive_quantity", "quantity > 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(i => i.LocationId)
            .HasColumnName("location_id")
            .IsRequired();

        builder.Property(i => i.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(i => i.User)
            .WithMany(u => u.Items)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Location)
            .WithMany(l => l.Items)
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(i => i.LocationId)
            .HasDatabaseName("idx_items_location");

        builder.HasIndex(i => i.UserId)
            .HasDatabaseName("idx_items_user");

        builder.HasIndex(i => i.Name)
            .HasDatabaseName("idx_items_name_trgm")
            .HasMethod("GIN")
            .HasOperators("gin_trgm_ops");
    }
}
