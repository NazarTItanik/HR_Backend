using HR_System.Data;
using HR_System.Enums;
using HR_System.Models;
using HR_System.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayslipsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PayslipsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("generatePayslip")]
        public async Task<IActionResult> GenerateBatchCustom([FromBody] GenerateCustomPayslipsDto request)
        {
            if (request.StartDate > request.EndDate)
                return BadRequest("Start date cannot be after end date.");

            var contractsQuery = _context.EmploymentContracts.Where(c => c.IsActive);
            if (request.EmployeeIds != null && request.EmployeeIds.Any())
            {
                contractsQuery = contractsQuery.Where(c => request.EmployeeIds.Contains(c.EmployeeId));
            }

            var activeContracts = await contractsQuery.ToListAsync();
            var targetEmployeeIds = activeContracts.Select(c => c.EmployeeId).ToList();

            if (!activeContracts.Any()) return BadRequest("No active contracts found.");

            var allAttendances = await _context.Attendances
                .Where(a => targetEmployeeIds.Contains(a.EmployeeId)
                         && a.Date.Date >= request.StartDate.Date
                         && a.Date.Date <= request.EndDate.Date
                         && a.Status == AttendanceStatus.Validated)
                .ToListAsync();

            var allLeaves = await _context.Leaves
                .Where(l => targetEmployeeIds.Contains(l.EmployeeId)
                         && l.StartDate.Date >= request.StartDate.Date
                         && l.EndDate.Date <= request.EndDate.Date)
                .ToListAsync();

            var generatedPayslips = new List<Payslip>();

            foreach (var contract in activeContracts)
            {
                var workedHours = allAttendances
                    .Where(a => a.EmployeeId == contract.EmployeeId)
                    .Sum(a => (decimal)a.TotalHoursWorked);

                var paidLeaveHours = allLeaves
                    .Where(l => l.EmployeeId == contract.EmployeeId && l.IsPaid)
                    .Sum(l => (decimal)l.TotalDays * 8m);

                var totalPayableHours = workedHours + paidLeaveHours;

                decimal expectedWeeklyHours = contract.WorkType == Enums.WorkType.PartTime ? 20m : 40m;
                decimal expectedMonthlyHours = expectedWeeklyHours * 4m;
                decimal effectiveHourlyRate = 0m;

                switch (contract.WageType)
                {
                    case Enums.WageType.Hourly:
                        effectiveHourlyRate = contract.BaseSalary;
                        break;
                    case Enums.WageType.Weekly:
                        effectiveHourlyRate = contract.BaseSalary / expectedWeeklyHours;
                        break;
                    case Enums.WageType.Monthly:
                    default:
                        effectiveHourlyRate = contract.BaseSalary / expectedMonthlyHours;
                        break;
                }

                decimal calculatedGross = effectiveHourlyRate * totalPayableHours;

                decimal taxRate = 0.20m;
                decimal calculatedNet = calculatedGross - (calculatedGross * taxRate);

                var newPayslip = new Payslip
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = contract.EmployeeId,
                    PeriodStart = request.StartDate,
                    PeriodEnd = request.EndDate,
                    GrossSalary = Math.Round(calculatedGross, 2),
                    NetSalary = Math.Round(calculatedNet, 2),
                    GenerationDate = DateTime.UtcNow,
                    Status = PayslipStatus.Unpaid
                };

                generatedPayslips.Add(newPayslip);
            }

            _context.Payslips.AddRange(generatedPayslips);
            await _context.SaveChangesAsync();

            return Ok(generatedPayslips);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payslip>>> GetAllPayslips()
        {
            var payslips = await _context.Payslips
                .OrderByDescending(p => p.GenerationDate)
                .ToListAsync();

            return Ok(payslips);
        }

        [HttpPost("delete-multiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("No IDs provided.");

            var payslipsToDelete = await _context.Payslips
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            if (!payslipsToDelete.Any())
                return NotFound("No matching records found.");

            _context.Payslips.RemoveRange(payslipsToDelete);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("employee/{employeeId:guid}")]
        public async Task<IActionResult> GetByEmployee(Guid employeeId)
        {
            var payslips = await _context.Payslips
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.PeriodEnd)
                .ToListAsync();

            return Ok(payslips);
        }
    }
}
