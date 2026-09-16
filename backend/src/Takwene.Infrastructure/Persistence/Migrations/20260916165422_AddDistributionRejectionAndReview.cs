using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Takwene.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributionRejectionAndReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "TrackDistributions",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "TrackDistributions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "TrackDistributions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "TrackDistributions");
        }
    }
}
