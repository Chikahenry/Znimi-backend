using LoanApplication.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class RepaymentSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        public int LoanId { get; set; }

        public int InstallmentNumber { get; set; }

        public DateTimeOffset DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InterestAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfterPayment { get; set; }

        public InterestPhase InterestPhase { get; set; }

        public bool IsPaid { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0m;

        public DateTime? PaidDate { get; set; }

        // Navigation
        [ForeignKey("LoanId")]
        public Loan Loan { get; set; }
    }
}
