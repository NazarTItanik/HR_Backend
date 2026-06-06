using HR_System.Data;
using HR_System.Repositories;
using HR_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers
{
    [ApiController]
    [Route("api/")]
    public class VacanciesController : Controller
    {


        private readonly IEmailService _emailService;


        private readonly AppDbContext _context;

        public VacanciesController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("GetVacancies")]
        public async Task<IActionResult> GetVacanciesAll()
        {
            var vacancies = await _context.Vacancies.ToListAsync();
            return Ok(vacancies);
        }
    }
}
