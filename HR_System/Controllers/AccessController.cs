using HR_System.Data;
using HR_System.DTOs;
using HR_System.Models.DTOs;
using HR_System.Models.Entities;
using HR_System.Providers;
using HR_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using Supabase.Gotrue;
using System.Text;

namespace HR_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IJwtProvider _tokenProvider;
        public AccessController(AppDbContext context, IEmailService emailService, IJwtProvider tokenProvider)
        {
            _context = context;
            _emailService = emailService;
            _tokenProvider = tokenProvider;
        }

        private static readonly List<JobApplication> _requests = new List<JobApplication>();

        [HttpPost("apply")]
        public IActionResult Apply([FromBody] JobApplication request)
        {
            _requests.Add(request);
            return Ok(new { Message = "Application has been succesfully applied" });
        }

        [HttpGet("list")]
        public IActionResult GetRequests()
        {
            return Ok(_requests);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterEmployeeDto request)
        {
            string rawPassword = CreatePassword(10);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);

            var newEmployee = new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = hashedPassword,
                Role = request.Role,
            };

            await _context.Employees.AddAsync(newEmployee);
            await _context.SaveChangesAsync();

            await _emailService.SendAsync(newEmployee.Email,"Dimon","Password", rawPassword);

            return Ok(new { message = "Employee created and email sent." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginEmployeeDto request)
        {

            var user = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email.ToLower() == request.Email.ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Incorrect email or password" });
            }

            return Ok(new
            {
                token = _tokenProvider.GenerateToken(user),
                User = user,
            });
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
