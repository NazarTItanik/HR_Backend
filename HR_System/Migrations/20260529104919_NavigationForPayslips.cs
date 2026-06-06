using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Migrations
{
    /// <inheritdoc />
    public partial class NavigationForPayslips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId",
                table: "Payslips",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_VacancyId",
                table: "Candidates",
                column: "VacancyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Vacancies_VacancyId",
                table: "Candidates",
                column: "VacancyId",
                principalTable: "Vacancies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payslips_Employees_EmployeeId",
                table: "Payslips",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Vacancies_VacancyId",
                table: "Candidates");

            migrationBuilder.DropForeignKey(
                name: "FK_Payslips_Employees_EmployeeId",
                table: "Payslips");

            migrationBuilder.DropIndex(
                name: "IX_Payslips_EmployeeId",
                table: "Payslips");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_VacancyId",
                table: "Candidates");
        }
    }
}
