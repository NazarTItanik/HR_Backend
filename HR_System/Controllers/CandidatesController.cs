using HR_System.Data;
using HR_System.Enums;
using HR_System.Models.DTOs;
using HR_System.Models.Entities;
using HR_System.Repositories;
using HR_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CandidatesController : ControllerBase
    {
        private readonly ICandidateRepository _repository;

        private readonly IEmailService _emailService;

        //public CandidatesController(ICandidateRepository repository, IEmailService emailService)
        //{
        //    _repository = repository;
        //    _emailService = emailService;
        //}

        private readonly AppDbContext _context;

        public CandidatesController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var candidates = await _context.Candidates.ToListAsync();
            return Ok(candidates);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateCandidateDto request)
        {
            string? resumePath = null;
            Console.WriteLine("0------------------------------------0");
            Console.WriteLine(request.resumeFile);
            Console.WriteLine(request.resumeFile.Length > 0);
            if (request.resumeFile != null)
            {
                var uploadsFolder = Path.Combine("wwwroot", "resumes");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{request.resumeFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await request.resumeFile.CopyToAsync(stream);

                resumePath = $"/resumes/{fileName}";
            }

            var newCandidate = new Candidate
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                VacancyId = request.VacancyId,
                resumeFile = resumePath,
                DateOfApplication = request.DateOfApplication ?? DateTime.UtcNow,
                Email = request.Email,
                ContactNumber = request.ContactNumber,
                Facebook = request.Facebook,
                Twitter = request.Twitter,
                LinkedIn = request.LinkedIn,
                Keywords = request.Keywords,
                Notes = request.Notes,
                Stage = CandidateStage.New,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Candidates.AddAsync(newCandidate);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Candidate added successfully",
                candidateId = newCandidate.Id
            });
        }

        [HttpPost("{id}/send-email")]
        public async Task<IActionResult> SendEmail(Guid id, [FromBody] SendEmailDto request)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null)
                return NotFound(new { message = "Candidate not found" });

            try
            {
                await _emailService.SendAsync(
                    candidate.Email,                                    // ← из БД
                    $"{candidate.FirstName} {candidate.LastName}",      // ← из БД
                    request.Subject,
                    request.Body
                );

                candidate.Stage = CandidateStage.Interviewing;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Email sent successfully",
                    newStage = candidate.Stage.ToString()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed for {id}. Error: {ex.Message}");
                return StatusCode(500, new { message = "Failed to send email. Stage was not updated." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCandidate(Guid id)
        {
            var candidate = await _context.Candidates.FindAsync(id);

            if (candidate == null)
            {
                return NotFound();
            }

            _context.Candidates.Remove(candidate);
            await _context.SaveChangesAsync(); 

            return NoContent();
        }

        [HttpGet("GetCandidate/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var candidate = await _context.Candidates.FindAsync(id);

            if (candidate == null)
                return NotFound();

            return Ok(candidate);
        }

        [HttpGet("{id}/resume")]
        public IActionResult GetResume(Guid id)
        {
            var candidate = _context.Candidates.Find(id);
            if (candidate == null || candidate.resumeFile == null)
                return NotFound();

            var filePath = Path.Combine("wwwroot", candidate.resumeFile.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf");
        }

        [HttpPost("Update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] CreateCandidateDto request)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null)
                return NotFound();

            candidate.FirstName = request.FirstName;
            candidate.MiddleName = request.MiddleName;
            candidate.LastName = request.LastName;
            candidate.VacancyId = request.VacancyId;
            candidate.DateOfApplication = request.DateOfApplication ?? candidate.DateOfApplication;
            candidate.Email = request.Email;
            candidate.ContactNumber = request.ContactNumber;
            candidate.Facebook = request.Facebook;
            candidate.Twitter = request.Twitter;
            candidate.LinkedIn = request.LinkedIn;
            candidate.Keywords = request.Keywords;
            candidate.Notes = request.Notes;
            candidate.Stage = request.Stage;

            // Update resume only if a new file is provided
            if (request.resumeFile != null && request.resumeFile.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "resumes");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{request.resumeFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await request.resumeFile.CopyToAsync(stream);

                candidate.resumeFile = $"/resumes/{fileName}";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Candidate updated successfully",
                candidateId = candidate.Id
            });
        }

        [HttpPost("{id}/stage")]
        public async Task<IActionResult> UpdateStage(Guid id, [FromBody] Dictionary<string, string> body)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null)
                return NotFound();

            if (!Enum.TryParse<CandidateStage>(body["stage"], out var parsedStage))
                return BadRequest("Invalid stage value");

            candidate.Stage = parsedStage;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Stage updated successfully" });
        }


    }
}