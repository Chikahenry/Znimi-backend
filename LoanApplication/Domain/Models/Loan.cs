using LoanApplication.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoanApplication.Domain.Models
{
    public class Loan
    {
        [Key]
        public int LoanId { get; set; }

        [Required, MaxLength(20)]
        public string LoanNumber { get; set; } // LN-2026-001

        public int BorrowerId { get; set; }

        public int CreatedByUserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RequestedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ApprovedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingPrincipal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInterestAccrued { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPenaltiesAccrued { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Phase1InterestRate { get; set; } = 5.0m; // 5% monthly

        [Column(TypeName = "decimal(5,2)")]
        public decimal Phase2InterestRate { get; set; } = 3.0m; // 3% monthly

        public int DurationInMonths { get; set; }

        public int GracePeriodDays { get; set; } = 3;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyPenaltyAmount { get; set; } = 500m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal PenaltyPercentage { get; set; } = 0m; // Alternative: 1% of overdue

        public LoanStatus Status { get; set; } = LoanStatus.Pending;

        public DateTimeOffset ApplicationDate { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ApprovalDate { get; set; }

        public DateTimeOffset? DisbursementDate { get; set; }

        public PaymentMethod? DisbursementMethod { get; set; }

        public int RepaymentDay { get; set; } = 15; // Day of month for payment (e.g., 15th)

        public DateTimeOffset? FirstPaymentDueDate { get; set; }

        public DateTimeOffset? LastPaymentDate { get; set; }

        public DateTimeOffset? ClosedDate { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        // Navigation
        [ForeignKey("BorrowerId")]
        public Borrower Borrower { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User CreatedBy { get; set; }

        [ForeignKey("ApprovedByUserId")]
        public User ApprovedBy { get; set; }

        [JsonIgnore]
        public ICollection<Payment> Payments { get; set; }
        [JsonIgnore]
        public ICollection<RepaymentSchedule> RepaymentSchedules { get; set; }
    }
}
