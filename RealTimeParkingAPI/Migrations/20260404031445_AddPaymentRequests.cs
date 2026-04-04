using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealTimeParkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ParkingHistories",
                table: "ParkingHistories");

            migrationBuilder.DropColumn(
                name: "QrCodeValue",
                table: "ParkingReservations");

            migrationBuilder.RenameTable(
                name: "ParkingHistories",
                newName: "ParkingHistory");

            migrationBuilder.RenameColumn(
                name: "DateTime",
                table: "ParkingHistory",
                newName: "ReservedAt");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "ParkingReservations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "ParkingReservations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "ParkingReservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmount",
                table: "ParkingReservations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReservationReference",
                table: "ParkingReservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MerchantDisplayName",
                table: "ParkingLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantGcashNumber",
                table: "ParkingLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantPaymentUrl",
                table: "ParkingLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantQrText",
                table: "ParkingLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProvider",
                table: "ParkingLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInAt",
                table: "ParkingHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutAt",
                table: "ParkingHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "ParkingHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParkingSlotId",
                table: "ParkingHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmount",
                table: "ParkingHistory",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "ParkingHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "ParkingHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "ParkingHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationReference",
                table: "ParkingHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParkingHistory",
                table: "ParkingHistory",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "PaymentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ParkingLocationId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalPaymentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MerchantDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MerchantGcashNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_ParkingLocations_ParkingLocationId",
                        column: x => x.ParkingLocationId,
                        principalTable: "ParkingLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_ParkingReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "ParkingReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHistory_ParkingLocationId",
                table: "ParkingHistory",
                column: "ParkingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHistory_ParkingSlotId",
                table: "ParkingHistory",
                column: "ParkingSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHistory_UserId",
                table: "ParkingHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ParkingLocationId",
                table: "PaymentRequests",
                column: "ParkingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ReservationId",
                table: "PaymentRequests",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_UserId",
                table: "PaymentRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingHistory_ParkingLocations_ParkingLocationId",
                table: "ParkingHistory",
                column: "ParkingLocationId",
                principalTable: "ParkingLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingHistory_ParkingSlots_ParkingSlotId",
                table: "ParkingHistory",
                column: "ParkingSlotId",
                principalTable: "ParkingSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingHistory_Users_UserId",
                table: "ParkingHistory",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingHistory_ParkingLocations_ParkingLocationId",
                table: "ParkingHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingHistory_ParkingSlots_ParkingSlotId",
                table: "ParkingHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingHistory_Users_UserId",
                table: "ParkingHistory");

            migrationBuilder.DropTable(
                name: "PaymentRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParkingHistory",
                table: "ParkingHistory");

            migrationBuilder.DropIndex(
                name: "IX_ParkingHistory_ParkingLocationId",
                table: "ParkingHistory");

            migrationBuilder.DropIndex(
                name: "IX_ParkingHistory_ParkingSlotId",
                table: "ParkingHistory");

            migrationBuilder.DropIndex(
                name: "IX_ParkingHistory_UserId",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "ParkingReservations");

            migrationBuilder.DropColumn(
                name: "PaymentAmount",
                table: "ParkingReservations");

            migrationBuilder.DropColumn(
                name: "ReservationReference",
                table: "ParkingReservations");

            migrationBuilder.DropColumn(
                name: "MerchantDisplayName",
                table: "ParkingLocations");

            migrationBuilder.DropColumn(
                name: "MerchantGcashNumber",
                table: "ParkingLocations");

            migrationBuilder.DropColumn(
                name: "MerchantPaymentUrl",
                table: "ParkingLocations");

            migrationBuilder.DropColumn(
                name: "MerchantQrText",
                table: "ParkingLocations");

            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                table: "ParkingLocations");

            migrationBuilder.DropColumn(
                name: "CheckInAt",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "CheckOutAt",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "ParkingSlotId",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "PaymentAmount",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "ParkingHistory");

            migrationBuilder.DropColumn(
                name: "ReservationReference",
                table: "ParkingHistory");

            migrationBuilder.RenameTable(
                name: "ParkingHistory",
                newName: "ParkingHistories");

            migrationBuilder.RenameColumn(
                name: "ReservedAt",
                table: "ParkingHistories",
                newName: "DateTime");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "ParkingReservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "ParkingReservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeValue",
                table: "ParkingReservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParkingHistories",
                table: "ParkingHistories",
                column: "Id");
        }
    }
}
