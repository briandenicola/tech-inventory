using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCoreApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    BeforePayload = table.Column<string>(type: "TEXT", maxLength: 32768, nullable: false),
                    AfterPayload = table.Column<string>(type: "TEXT", maxLength: 32768, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Website = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Households",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    DefaultCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ImportedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    RowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorLog = table.Column<string>(type: "TEXT", maxLength: 32768, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    EntraObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Color = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BrandId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NetworkId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    RetiredDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DisposalMethod = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTags",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTags", x => new { x.DeviceId, x.TagId });
                    table.ForeignKey(
                        name: "FK_DeviceTags_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Audit history queries filter by entity and time, so this composite index keeps append-only lookups efficient.
            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityType_EntityId_Timestamp",
                table: "AuditEvents",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            // Admin audit timelines sort and page by timestamp, so this index supports chronological scans.
            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Timestamp",
                table: "AuditEvents",
                column: "Timestamp");

            // Brand names are reference-data keys and must stay unique for case-insensitive lookups.
            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            // Category list queries group siblings by parent, so this index supports tree traversal and uniqueness checks.
            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId_Name",
                table: "Categories",
                columns: new[] { "ParentId", "Name" });

            // Device list filtering commonly joins to Brand, so keep the foreign-key path indexed.
            migrationBuilder.CreateIndex(
                name: "IX_Devices_BrandId",
                table: "Devices",
                column: "BrandId");

            // Device list filtering commonly joins to Category, so keep the foreign-key path indexed.
            migrationBuilder.CreateIndex(
                name: "IX_Devices_CategoryId",
                table: "Devices",
                column: "CategoryId");

            // Device list filtering commonly joins to Location, so keep the foreign-key path indexed.
            migrationBuilder.CreateIndex(
                name: "IX_Devices_LocationId",
                table: "Devices",
                column: "LocationId");

            // Device list filtering optionally joins to Network, so keep the nullable foreign-key path indexed.
            migrationBuilder.CreateIndex(
                name: "IX_Devices_NetworkId",
                table: "Devices",
                column: "NetworkId");

            // Device ownership changes and filters join on Owner, so keep the foreign-key path indexed.
            migrationBuilder.CreateIndex(
                name: "IX_Devices_OwnerId",
                table: "Devices",
                column: "OwnerId");

            // Device-tag queries frequently fetch all devices for a tag, so index the trailing key for reverse lookups.
            migrationBuilder.CreateIndex(
                name: "IX_DeviceTags_TagId",
                table: "DeviceTags",
                column: "TagId");

            // Household names are operator-managed reference data and must stay unique.
            migrationBuilder.CreateIndex(
                name: "IX_Households_Name",
                table: "Households",
                column: "Name",
                unique: true);

            // Import history is reviewed by status and recency, so this index keeps batch dashboards efficient.
            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Status_CreatedAt",
                table: "ImportBatches",
                columns: new[] { "Status", "CreatedAt" });

            // Location names are operator-managed reference data and must stay unique for lookups.
            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name",
                table: "Locations",
                column: "Name",
                unique: true);

            // Network names are operator-managed reference data and must stay unique for lookups.
            migrationBuilder.CreateIndex(
                name: "IX_Networks_Name",
                table: "Networks",
                column: "Name",
                unique: true);

            // Owner display names are managed reference data and should resolve to a single record.
            migrationBuilder.CreateIndex(
                name: "IX_Owners_DisplayName",
                table: "Owners",
                column: "DisplayName",
                unique: true);

            // Entra object IDs must map to at most one owner record.
            migrationBuilder.CreateIndex(
                name: "IX_Owners_EntraObjectId",
                table: "Owners",
                column: "EntraObjectId",
                unique: true);

            // Tag names are operator-managed reference data and must stay unique for lookups.
            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "DeviceTags");

            migrationBuilder.DropTable(
                name: "Households");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Networks");

            migrationBuilder.DropTable(
                name: "Owners");
        }
    }
}
