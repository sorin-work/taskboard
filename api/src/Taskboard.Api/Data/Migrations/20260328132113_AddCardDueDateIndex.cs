using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taskboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardDueDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cards_board_id",
                table: "cards");

            migrationBuilder.CreateIndex(
                name: "ix_cards_board_id_due_date",
                table: "cards",
                columns: new[] { "board_id", "due_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cards_board_id_due_date",
                table: "cards");

            migrationBuilder.CreateIndex(
                name: "ix_cards_board_id",
                table: "cards",
                column: "board_id");
        }
    }
}
