using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddYandexFieldsToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Routes_RouteId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Stops_StopId",
                table: "Subscriptions");

            migrationBuilder.AlterColumn<int>(
                name: "StopId",
                table: "Subscriptions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "RouteId",
                table: "Subscriptions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ExternalRouteNumber",
                table: "Subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalStopCode",
                table: "Subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramId",
                table: "Users",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stops_ExternalId",
                table: "Stops",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ExternalId",
                table: "Routes",
                column: "ExternalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Routes_RouteId",
                table: "Subscriptions",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Stops_StopId",
                table: "Subscriptions",
                column: "StopId",
                principalTable: "Stops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Routes_RouteId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Stops_StopId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Users_TelegramId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Stops_ExternalId",
                table: "Stops");

            migrationBuilder.DropIndex(
                name: "IX_Routes_ExternalId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ExternalRouteNumber",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalStopCode",
                table: "Subscriptions");

            migrationBuilder.AlterColumn<int>(
                name: "StopId",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RouteId",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Routes_RouteId",
                table: "Subscriptions",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Stops_StopId",
                table: "Subscriptions",
                column: "StopId",
                principalTable: "Stops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
