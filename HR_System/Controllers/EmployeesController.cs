using HR_System.Data;
using HR_System.Enums;
using HR_System.Models.DTOs;
using HR_System.Models.Entities;
using HR_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HR_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        // Внедрение зависимости (Dependency Injection) базы данных
        public EmployeesController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("CreateEmployee")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            string rawPassword = CreatePassword(10);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);

            

            var employee = new Employee
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Role = dto.Role,
                ContactNumber = dto.ContactNumber,
                PasswordHash = hashedPassword,
                HireDate = dto.HireDate,
                Status = "Active"
            };

            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            string body = $@"Dear {employee.FirstName} {employee.LastName},

We are delighted to welcome you to the team! Following the successful completion of the interview process, we are pleased to confirm that you have been selected for the position you applied for.

Your account in our HR Management System has been created. Please find your login credentials below:

   Email:    {employee.Email}
   Password: {rawPassword}

For security reasons, we strongly recommend that you sign in at your earliest convenience and change the temporary password through your profile settings.

Through the system, you will be able to track your attendance, request leave, view your payslips and access other resources related to your employment.

If you have any questions or encounter any difficulties accessing your account, please do not hesitate to contact the HR Department.

Once again, welcome aboard — we look forward to working with you.

Best regards,
HR Department";

            await _emailService.SendAsync(employee.Email, "Dimon", "Password", body);

            var contract = new EmploymentContract
            {
                EmployeeId = employee.Id, 
                VacancyId = dto.VacancyId,
                BaseSalary = dto.BaseSalary,
                WorkType = dto.WorkType,
                WageType = dto.WageType,
                StartDate = dto.HireDate.ToUniversalTime(),
                IsActive = true
            };

            _context.EmploymentContracts.Add(contract);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee has been added" });
        }

        [HttpPost("Update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.Email = dto.Email;
            employee.Status = dto.Status;
            employee.Role = dto.Role;

            await _context.SaveChangesAsync();
            return Ok(employee);
        }

        [HttpPost("delete-multiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            // 1. Fetch all records that exist in the database
            var employees = await _context.Employees
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();

            if (!employees.Any())
            {
                return NotFound("No employees found with the provided IDs.");
            }

            // 2. Remove the entire batch
            _context.Employees.RemoveRange(employees);

            // 3. Save changes
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Successfully deleted {employees.Count} employees." });
        }

        // GET: api/employees
        [HttpGet("GetEmployees")]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employees = await _context.Employees.Include(e => e.Contracts.Where(c => c.IsActive)).ThenInclude(contract => contract.Vacancy).ToListAsync();

            if (employees == null || !employees.Any())
            {
                return NotFound("No Employees Found");
            }

            return Ok(employees);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var employee = await _context.Employees
                .Include(e => e.Contracts)
                    .ThenInclude(c => c.Vacancy)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            return Ok(employee);
        }

        private string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }
    }
}