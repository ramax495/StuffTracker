using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffTracker.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPathIdsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new column with default empty array
            migrationBuilder.AddColumn<Guid[]>(
                name: "path_ids",
                table: "storage_locations",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'::uuid[]");

            // Populate path_ids for existing locations using a recursive CTE
            // This builds the ID path from root to each location
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
                WHERE sl.id = lp.id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "path_ids",
                table: "storage_locations");
        }
    }
}
