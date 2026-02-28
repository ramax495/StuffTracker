using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffTracker.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEmptyPathIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix any locations that have empty path_ids but should have values
            // This can happen if locations were created before the PathIds migration
            // or if the migration data population script failed
            migrationBuilder.Sql(@"
                WITH RECURSIVE location_paths AS (
                    -- Base case: top-level locations (no parent)
                    SELECT
                        id,
                        ARRAY[id] AS path_ids
                    FROM storage_locations
                    WHERE parent_id IS NULL

                    UNION ALL

                    -- Recursive case: child locations
                    SELECT
                        sl.id,
                        lp.path_ids || sl.id
                    FROM storage_locations sl
                    INNER JOIN location_paths lp ON sl.parent_id = lp.id
                )
                UPDATE storage_locations sl
                SET path_ids = lp.path_ids
                FROM location_paths lp
                WHERE sl.id = lp.id
                  AND (sl.path_ids IS NULL OR cardinality(sl.path_ids) = 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration: set path_ids back to empty for affected rows
            // This is a no-op since we only want to fix missing data
        }
    }
}
