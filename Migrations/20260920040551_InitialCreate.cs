using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace olhuz.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_USER",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CPF = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "date", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RecoveryToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TokenExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TokenUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_USER", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_USER_PREFERENCES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ScreenReader = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SpeechRate = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false, defaultValue: 1.0m),
                    VoiceType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VolumeLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    Theme = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VibrationEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AlertSoundEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_USER_PREFERENCES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_USER_PREFERENCES_TB_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "TB_USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_USER_CPF",
                table: "TB_USER",
                column: "CPF",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TB_USER_Email",
                table: "TB_USER",
                column: "Email",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TB_USER_PhoneNumber",
                table: "TB_USER",
                column: "PhoneNumber",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TB_USER_PREFERENCES_UserId",
                table: "TB_USER_PREFERENCES",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_USER_PREFERENCES");

            migrationBuilder.DropTable(
                name: "TB_USER");
        }
    }
}
