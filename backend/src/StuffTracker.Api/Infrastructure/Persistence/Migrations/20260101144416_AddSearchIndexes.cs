using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffTracker.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Ensures pg_trgm extension is enabled for fuzzy text search.
    /// Note: GIN indexes on items.name and storage_locations.name using gin_trgm_ops
    /// were created in InitialCreate migration.
    /// </summary>
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pg_trgm extension for fuzzy text search
            // This is idempotent - will not fail if already exists
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: We don't drop the extension in Down as it may be used by other
            // parts of the database. Extensions are typically left in place.
            // If needed: migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}
