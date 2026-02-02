using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaybillManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSupplierToCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Waybills_Suppliers_SupplierId",
                table: "Waybills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Waybills_TenantId_SupplierId",
                table: "Waybills",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Waybills_Suppliers_TenantId_SupplierId",
                table: "Waybills",
                columns: new[] { "TenantId", "SupplierId" },
                principalTable: "Suppliers",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Waybills_Suppliers_TenantId_SupplierId",
                table: "Waybills");

            migrationBuilder.DropIndex(
                name: "IX_Waybills_TenantId_SupplierId",
                table: "Waybills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Waybills_Suppliers_SupplierId",
                table: "Waybills",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
