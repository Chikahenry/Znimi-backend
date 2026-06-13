using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanApplication.Migrations
{
    /// <inheritdoc />
    public partial class messageWhatsapp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppMessages_Borrowers_BorrowerId",
                table: "WhatsAppMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppMessages_Loans_LoanId",
                table: "WhatsAppMessages");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppMessages_BorrowerId",
                table: "WhatsAppMessages");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppMessages_LoanId",
                table: "WhatsAppMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_BorrowerId",
                table: "WhatsAppMessages",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_LoanId",
                table: "WhatsAppMessages",
                column: "LoanId");

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppMessages_Borrowers_BorrowerId",
                table: "WhatsAppMessages",
                column: "BorrowerId",
                principalTable: "Borrowers",
                principalColumn: "BorrowerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppMessages_Loans_LoanId",
                table: "WhatsAppMessages",
                column: "LoanId",
                principalTable: "Loans",
                principalColumn: "LoanId");
        }
    }
}
