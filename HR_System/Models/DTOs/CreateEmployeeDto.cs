using HR_System.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace HR_System.Models.DTOs
{
    public class CreateEmployeeDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public DateTime HireDate { get; set; } = DateTime.Now;


        [Required]
        public int VacancyId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [Range(0, 1000000, ErrorMessage = "Salary must be a positive number.")]
        public decimal BaseSalary { get; set; }

        [Required]
        public WorkType WorkType { get; set; } = WorkType.FullTime;

        [Required]
        public WageType WageType { get; set; } = WageType.Monthly;

        [Required]
        public string Role { get; set; }
    }
}