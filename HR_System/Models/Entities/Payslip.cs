using HR_System.Enums;
using HR_System.Models.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_System.Models
{
    public class Payslip
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; } 

        public DateTime GenerationDate { get; set; }
        public PayslipStatus Status { get; set; } = PayslipStatus.Unpaid;
    }
}