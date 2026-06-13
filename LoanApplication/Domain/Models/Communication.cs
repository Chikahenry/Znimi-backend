using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class Communication
    {
        [Key]
        public int CommunicationId { get; set; }

        public int BorrowerId { get; set; }

        public int? LoanId { get; set; }

        public int? SentByUserId { get; set; }

        [Required, MaxLength(50)]
        public string CommunicationType { get; set; } // SMS, WhatsApp, Email, Call

        [Required]
        public string Message { get; set; }

        public bool IsAutomated { get; set; } = false;

        public bool IsSent { get; set; } = false;

        public DateTimeOffset ScheduledAt { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        [MaxLength(200)]
        public string ErrorMessage { get; set; }

        // Navigation
        [ForeignKey("BorrowerId")]
        public Borrower Borrower { get; set; }

        [ForeignKey("LoanId")]
        public Loan Loan { get; set; }

        [ForeignKey("SentByUserId")]
        public User SentBy { get; set; }
    }
}
