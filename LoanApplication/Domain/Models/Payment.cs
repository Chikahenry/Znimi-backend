using LoanApplication.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required, MaxLength(20)]
        public string PaymentNumber { get; set; } // PAY-2026-001

        public int LoanId { get; set; }

        public int RecordedByUserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PenaltyPortion { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal InterestPortion { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalPortion { get; set; } = 0m;

        public PaymentMethod PaymentMethod { get; set; }

        public DateTimeOffset PaymentDate { get; set; }

        public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

        [MaxLength(500)]
        public string Notes { get; set; }

        [MaxLength(100)]
        public string ReceiptNumber { get; set; }

        // Navigation
        [ForeignKey("LoanId")]
        public Loan Loan { get; set; }

        [ForeignKey("RecordedByUserId")]
        public User RecordedBy { get; set; }
    }
}
