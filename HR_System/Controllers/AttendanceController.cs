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
            // 1. Try to convert the string from the URL into your Enum (case-insensitive)
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

        //[HttpPost("validate/{id}")]
        //public async Task<IActionResult> Validate(Guid id)
        //{
        //    var record = await _context.Attendances.FindAsync(id);
        //    if (record == null) return NotFound();

        //    record.Status = "Validated";
        //    await _context.SaveChangesAsync();

        //    return Ok();
        //}
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

            // Fix: Clear navigation property to prevent JSON circular reference errors
            model.Employee = null;

            // 1. Calculate hours (your existing logic)
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

            // 2. Enum Validation Fix
            // Enums default to 0. If 0 is AttendanceStatus.Pending, this is how you check it:
            if (model.Status == default(AttendanceStatus))
            {
                model.Status = AttendanceStatus.Pending;
            }

            // 3. Set current date if missing
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

            // No more magic strings!
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

            // Update the status to Rejected
            // Ensure 'AttendanceStatus.Rejected' matches your enum definition
            attendance.Status = AttendanceStatus.Rejected;

            // Save changes
            await _context.SaveChangesAsync();

            return Ok(attendance);
        }

        [HttpPost("delete-multiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("No IDs provided.");

            // Find all records that match the provided IDs
            var records = await _context.Attendances
                .Where(a => ids.Contains(a.Id)) // Ensure this matches your ID property name
                .ToListAsync();

            if (!records.Any()) return NotFound("No records found.");

            _context.Attendances.RemoveRange(records);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Deleted {records.Count} records." });
        }

        [HttpPost("update")] 
        public async Task<IActionResult> Update([FromBody] Attendance model)
        {
            // 1. Find the existing record by ID
            var existing = await _context.Attendances.FindAsync(model.Id);
            if (existing == null)
            {
                return NotFound($"Attendance record with ID {model.Id} not found.");
            }

            // 2. Map the updated fields
            existing.EmployeeId = model.EmployeeId;
            existing.Date = model.Date;
            existing.ClockIn = model.ClockIn;
            existing.ClockOut = model.ClockOut;
            existing.Status = model.Status;

            // 3. Re-calculate logic (Crucial: update total hours if times changed)
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

            // 4. Save changes
            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        // POST: api/Attendance/{id}
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttendanceDto dto)
        {
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
                return NotFound(new { message = $"Attendance with id {id} not found." });

            // Обновляем clockOut если передан
            if (dto.ClockOut.HasValue)
            {
                attendance.ClockOut = dto.ClockOut;

                // Автоматически вычисляем total hours worked
                if (attendance.ClockIn.HasValue && attendance.ClockOut.HasValue)
                {
                    var duration = attendance.ClockOut.Value - attendance.ClockIn.Value;
                    attendance.TotalHoursWorked = (double)duration.TotalHours;
                }
            }

            // Обновляем clockIn если передан
            if (dto.ClockIn.HasValue)
            {
                attendance.ClockIn = dto.ClockIn;
            }

            await _context.SaveChangesAsync();

            return Ok(attendance);
        }

        // GET: api/Attendance/employee/{employeeId}?date=2026-06-02
        [HttpGet("employee/{employeeId:guid}")]
        public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] DateTime? date)
        {
            var query = _context.Attendances
                .Where(a => a.EmployeeId == employeeId);

            if (date.HasValue)
            {
                // Сравниваем только дату (без времени)
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
