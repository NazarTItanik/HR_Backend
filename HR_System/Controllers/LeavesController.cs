namespace HR_System.Controllers
{
    using HR_System.Data;
    using HR_System.Enums;
    using HR_System.Models.DTOs;
    using HR_System.Models.Entities; 
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


            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            return Ok(leave);
        }


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

            var records = await _context.Leaves
                .Where(l => ids.Contains(l.Id))
                .ToListAsync();

            if (!records.Any())
            {
                return NotFound("No leave requests found with the provided IDs.");
            }


            _context.Leaves.RemoveRange(records);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Successfully deleted {records.Count} records." });
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<Leave>>> GetLeavesByStatus(LeaveStatus status)
        {

            Console.WriteLine($"DEBUG: Received status request: {status}");


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

        [HttpPost("approve-multiple")]
        public async Task<IActionResult> ApproveMultiple([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("No IDs provided.");

            var records = await _context.Leaves
                .Where(l => ids.Contains(l.Id))
                .ToListAsync();

            if (!records.Any())
                return NotFound("No leave requests found with the provided IDs.");

            var skipped = new List<Guid>();

            foreach (var leave in records)
            {
                if (leave.Status == LeaveStatus.Approved)
                {
                    skipped.Add(leave.Id);
                    continue;
                }

                leave.Status = LeaveStatus.Approved;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Approved {records.Count - skipped.Count} leave request(s).",
                approvedCount = records.Count - skipped.Count,
                skippedCount = skipped.Count,
                skippedIds = skipped,
                notFoundIds = ids.Except(records.Select(r => r.Id)).ToList()
            });
        }

        [HttpPost("reject-multiple")]
        public async Task<IActionResult> RejectMultiple([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("No IDs provided.");

            var records = await _context.Leaves
                .Where(l => ids.Contains(l.Id))
                .ToListAsync();

            if (!records.Any())
                return NotFound("No leave requests found with the provided IDs.");

            var skipped = new List<Guid>();

            foreach (var leave in records)
            {
                if (leave.Status == LeaveStatus.Rejected)
                {
                    skipped.Add(leave.Id);
                    continue;
                }

                leave.Status = LeaveStatus.Rejected;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Rejected {records.Count - skipped.Count} leave request(s).",
                rejectedCount = records.Count - skipped.Count,
                skippedCount = skipped.Count,
                skippedIds = skipped,
                notFoundIds = ids.Except(records.Select(r => r.Id)).ToList()
            });
        }
    }
}
