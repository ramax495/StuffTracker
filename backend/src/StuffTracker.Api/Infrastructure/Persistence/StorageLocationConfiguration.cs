using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence;

public class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable("storage_locations", t =>
        {
            t.HasCheckConstraint("no_self_parent", "id != parent_id");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(l => l.ParentId)
            .HasColumnName("parent_id");

        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.PathNames)
            .HasColumnName("path_names")
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'::text[]")
            .IsRequired();

        builder.Property(l => l.PathIds)
            .HasColumnName("path_ids")
            .HasColumnType("uuid[]")
            .HasDefaultValueSql("'{}'::uuid[]")
            .IsRequired();

        builder.Property(l => l.Depth)
            .HasColumnName("depth")
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(l => l.User)
            .WithMany(u => u.StorageLocations)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Parent)
            .WithMany(l => l.Children)
            .HasForeignKey(l => l.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(l => new { l.UserId, l.ParentId })
            .HasDatabaseName("idx_locations_user_parent");

        builder.HasIndex(l => new { l.UserId, l.Depth })
            .HasDatabaseName("idx_locations_user_depth");

        builder.HasIndex(l => l.Name)
            .HasDatabaseName("idx_locations_name_trgm")
            .HasMethod("GIN")
            .HasOperators("gin_trgm_ops");
    }
}
