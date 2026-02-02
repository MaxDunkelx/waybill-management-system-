using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaybillManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddErpSyncStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErpSyncStatus",
                table: "Waybills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastErpSyncAttemptAt",
                table: "Waybills",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Waybills_ErpSyncStatus",
                table: "Waybills",
                column: "ErpSyncStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Waybills_ErpSyncStatus",
                table: "Waybills");

            migrationBuilder.DropColumn(
                name: "ErpSyncStatus",
                table: "Waybills");

            migrationBuilder.DropColumn(
                name: "LastErpSyncAttemptAt",
                table: "Waybills");
        }
    }
}
