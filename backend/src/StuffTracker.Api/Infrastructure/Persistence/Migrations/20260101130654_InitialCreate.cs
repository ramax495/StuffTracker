using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffTracker.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pg_trgm extension for fuzzy text search (required for gin_trgm_ops indexes)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.telegram_id);
                });

            migrationBuilder.CreateTable(
                name: "storage_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    path_names = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    depth = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_locations", x => x.id);
                    table.CheckConstraint("no_self_parent", "id != parent_id");
                    table.ForeignKey(
                        name: "FK_storage_locations_storage_locations_parent_id",
                        column: x => x.parent_id,
                        principalTable: "storage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_storage_locations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "telegram_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.CheckConstraint("positive_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_items_storage_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "storage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "telegram_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_items_location",
                table: "items",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "idx_items_name_trgm",
                table: "items",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_items_user",
                table: "items",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_locations_name_trgm",
                table: "storage_locations",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_locations_user_depth",
                table: "storage_locations",
                columns: new[] { "user_id", "depth" });

            migrationBuilder.CreateIndex(
                name: "idx_locations_user_parent",
                table: "storage_locations",
                columns: new[] { "user_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_storage_locations_parent_id",
                table: "storage_locations",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "storage_locations");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
