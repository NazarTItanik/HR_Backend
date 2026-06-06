using HR_System.Data;
using HR_System.Enums;
using HR_System.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context) { 
        _context = context;
        }

        [HttpGet("{status}")]
        public async Task<IActionResult> GetByStatus(string status)
        {
            if (!Enum.TryParse<AttendanceStatus>(status, true, out var parsedStatus))
            {
                return BadRequest(new { message = $"Status '{status}' is not valid." });
            }

            // 2. Query the database using the parsed enum
            var list = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Status == parsedStatus)
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Attendances.Include(a => a.Employee).ToListAsync();
            return Ok(list);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Attendance model)
        {
            if (model == null)
            {
                return BadRequest("Data is null");
            }

            model.Employee = null;


            if (model.ClockIn.HasValue && model.ClockOut.HasValue)
            {
                var duration = model.ClockOut.Value >= model.ClockIn.Value
                    ? model.ClockOut.Value - model.ClockIn.Value
                    : (TimeSpan.FromDays(1) - model.ClockIn.Value.TimeOfDay) + model.ClockOut.Value.TimeOfDay;

                model.TotalHoursWorked = duration.TotalHours;
            }
            else
            {
                model.TotalHoursWorked = 0;
            }


            if (model.Status == default(AttendanceStatus))
            {
                model.Status = AttendanceStatus.Pending;
            }

            if (model.Date == DateTime.MinValue)
            {
                model.Date = DateTime.Today;
            }

            _context.Attendances.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        [HttpPost("validate/{id}")]
        public async Task<IActionResult> ValidateAttendance(Guid id)
        {
            var attendance = await _context.Attendances.FindAsync(id);

            if (attendance == null) return NotFound();

            if (attendance.Status == AttendanceStatus.Validated)
            {
                return BadRequest("Already validated.");
            }

            attendance.Status = AttendanceStatus.Validated;
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(Guid id)
        {
            // Find the record
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound("Attendance record not found.");
            }

            attendance.Status = AttendanceStatus.Rejected;

            // Save changes
            await _context.SaveChangesAsync();

            return Ok(attendance);
        }

        [HttpPost("delete-multiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("No IDs provided.");

            var records = await _context.Attendances
                .Where(a => ids.Contains(a.Id)) 
                .ToListAsync();

            if (!records.Any()) return NotFound("No records found.");

            _context.Attendances.RemoveRange(records);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Deleted {records.Count} records." });
        }

        [HttpPost("update")] 
        public async Task<IActionResult> Update([FromBody] Attendance model)
        {

            var existing = await _context.Attendances.FindAsync(model.Id);
            if (existing == null)
            {
                return NotFound($"Attendance record with ID {model.Id} not found.");
            }


            existing.EmployeeId = model.EmployeeId;
            existing.Date = model.Date;
            existing.ClockIn = model.ClockIn;
            existing.ClockOut = model.ClockOut;
            existing.Status = model.Status;

            if (existing.ClockIn.HasValue && existing.ClockOut.HasValue)
            {
                var duration = existing.ClockOut.Value >= existing.ClockIn.Value
                    ? existing.ClockOut.Value - existing.ClockIn.Value
                    : (TimeSpan.FromDays(1) - existing.ClockIn.Value.TimeOfDay) + existing.ClockOut.Value.TimeOfDay;
                existing.TotalHoursWorked = duration.TotalHours;
            }
            else
            {
                existing.TotalHoursWorked = 0;
            }


            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        [HttpPost("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttendanceDto dto)
        {
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
                return NotFound(new { message = $"Attendance with id {id} not found." });


            if (dto.ClockOut.HasValue)
            {
                attendance.ClockOut = dto.ClockOut;

                if (attendance.ClockIn.HasValue && attendance.ClockOut.HasValue)
                {
                    var duration = attendance.ClockOut.Value - attendance.ClockIn.Value;
                    attendance.TotalHoursWorked = (double)duration.TotalHours;
                }
            }


            if (dto.ClockIn.HasValue)
            {
                attendance.ClockIn = dto.ClockIn;
            }

            await _context.SaveChangesAsync();

            return Ok(attendance);
        }


        [HttpGet("employee/{employeeId:guid}")]
        public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] DateTime? date)
        {
            var query = _context.Attendances
                .Where(a => a.EmployeeId == employeeId);

            if (date.HasValue)
            {

                var targetDate = date.Value.Date;
                query = query.Where(a => a.Date.Date == targetDate);
            }

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return Ok(attendances);
        }


    }
}
