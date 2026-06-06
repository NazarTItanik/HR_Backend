using HR_System.Data;
using HR_System.Enums;
using HR_System.Models.DTOs;
using HR_System.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContractsController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(Guid employeeId)
        {
            var contracts = await _context.EmploymentContracts
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            return Ok(contracts);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContractCreateDto dto)
        {

            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null) return NotFound("Employee not found.");


            var activeContracts = await _context.EmploymentContracts
                .Where(c => c.EmployeeId == dto.EmployeeId && c.IsActive)
                .ToListAsync();

            foreach (var contract in activeContracts)
            {
                contract.IsActive = false;
                contract.EndDate = DateTime.Now;
            }

    
            var newContract = new EmploymentContract
            {
                Id = Guid.NewGuid(),
                EmployeeId = dto.EmployeeId,
                VacancyId = dto.VacancyId,
                BaseSalary = dto.BaseSalary,
                StartDate = dto.StartDate,
                WorkType = dto.WorkType,
                WageType = dto.WageType,
                IsActive = true 
            };

            _context.EmploymentContracts.Add(newContract);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contract created successfully." });
        }
    }
}
