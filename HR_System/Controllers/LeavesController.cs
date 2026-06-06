namespace HR_System.Controllers
{
    using HR_System.Data; // Your DbContext namespace
    using HR_System.Enums;
    using HR_System.Models.DTOs;
    using HR_System.Models.Entities; // Your Entity namespace
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [Route("api/[controller]")]
    [ApiController]
    public class LeavesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeavesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveCreateDTO model) 
        {
            // 1. Map DTO to Entity (or map manually)
            var leave = new Leave
            {
                EmployeeId = model.EmployeeId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                LeaveType = model.LeaveType,
                Reason = model.Reason,
                Status = LeaveStatus.Pending 
            };

            leave.IsPaid = (leave.LeaveType != LeaveType.Unpaid);

            var diff = (leave.EndDate - leave.StartDate).TotalDays + 1;
            leave.TotalDays = (decimal)diff;

            // 4. Save to DB
            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            return Ok(leave);
        }

        // POST: api/Leaves/update
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] LeaveUpdateDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingLeave = await _context.Leaves.FindAsync(model.Id);

            if (existingLeave == null)
            {
                return NotFound($"Leave record with ID {model.Id} not found.");
            }

            // 1. Update the fields
            existingLeave.EmployeeId = model.EmployeeId;
            existingLeave.StartDate = model.StartDate;
            existingLeave.EndDate = model.EndDate;
            existingLeave.LeaveType = model.LeaveType;
            existingLeave.Reason = model.Reason;

            // 2. CRITICAL: Recalculate business logic
            // If the user changed the dates, TotalDays must update.
            // If the user changed the type, IsPaid must update.
            existingLeave.IsPaid = (model.LeaveType != LeaveType.Unpaid);

            var diff = (model.EndDate - model.StartDate).TotalDays + 1;
            existingLeave.TotalDays = (decimal)diff;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Error updating the database.");
            }

            return Ok(existingLeave);
        }

        [HttpPost("delete-multiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            // 1. Fetch all records that match the provided IDs
            var records = await _context.Leaves
                .Where(l => ids.Contains(l.Id))
                .ToListAsync();

            if (!records.Any())
            {
                return NotFound("No leave requests found with the provided IDs.");
            }

            // 2. Remove the batch and save
            _context.Leaves.RemoveRange(records);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Successfully deleted {records.Count} records." });
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<Leave>>> GetLeavesByStatus(LeaveStatus status)
        {
            // 1. Correct logging: Use the ILogger if available, or stay with Console 
            // (Check your Backend Terminal/Output window, NOT the browser!)
            Console.WriteLine($"DEBUG: Received status request: {status}");

            // 2. Remove the premature return that was killing your code
            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .Where(l => l.Status == status)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            return Ok(leaves);
        }

        [HttpPost("validate/{id}")]
        public async Task<IActionResult> Validate(Guid id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null) return NotFound("Leave request not found.");

            // Update status to Approved
            leave.Status = LeaveStatus.Approved;

            await _context.SaveChangesAsync();
            return Ok(leave);
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(Guid id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null) return NotFound("Leave request not found.");

            // Update status to Rejected
            leave.Status = LeaveStatus.Rejected;

            await _context.SaveChangesAsync();
            return Ok(leave);
        }

        [HttpGet("employee/{employeeId:guid}/pending")]
        public async Task<IActionResult> GetPendingByEmployee(Guid employeeId)
        {
            var pending = await _context.Leaves
                .Where(l => l.EmployeeId == employeeId
                         && l.Status == LeaveStatus.Pending)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            return Ok(pending);
        }

        [HttpGet("employee/{employeeId:guid}/approved")]
        public async Task<IActionResult> GetApprovedByEmployee(Guid employeeId)
        {
            var currentYear = DateTime.UtcNow.Year;

            var approved = await _context.Leaves
                .Where(l => l.EmployeeId == employeeId
                         && l.Status == LeaveStatus.Approved
                         && l.StartDate.Year == currentYear)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            return Ok(approved);
        }


        [HttpGet("employee/{employeeId:guid}")]
        public async Task<IActionResult> GetAllByEmployee(Guid employeeId)
        {
            var leaves = await _context.Leaves
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            return Ok(leaves);
        }
    }
}
