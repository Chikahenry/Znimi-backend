using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LoanApplication.Migrations
{
    /// <inheritdoc />
    public partial class addedDailySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailySnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalLoansOutstanding = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActiveLoansCount = table.Column<int>(type: "integer", nullable: false),
                    OverdueLoansCount = table.Column<int>(type: "integer", nullable: false),
                    InterestIncomeMonthToDate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PAR7Percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PAR30Percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CollectionRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySnapshots", x => x.SnapshotId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailySnapshots");
        }
    }
}
