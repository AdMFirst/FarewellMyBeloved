using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarewellMyBeloved.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToFarewellPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "FarewellPeople",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "FarewellPeople");
        }
    }
}
