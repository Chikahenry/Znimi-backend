using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoanApplication.Domain.Models
{
    public class WhatsAppMessage
    {
        [Key]
        public int MessageId { get; set; }

        public int BorrowerId { get; set; }

        public int? LoanId { get; set; }

        [Required, MaxLength(20)]
        public string RecipientPhoneNumber { get; set; }

        [Required]
        public string MessageBody { get; set; }

        [MaxLength(50)]
        public string MessageType { get; set; } // Reminder, PaymentConfirmation, Statement, etc.

        public bool IsSent { get; set; } = false;

        [MaxLength(100)]
        public string MessageSid { get; set; } // Twilio message ID

        [MaxLength(50)]
        public string Status { get; set; } // queued, sent, delivered, failed, read

        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? ReadAt { get; set; }

        [MaxLength(500)]
        public string ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //// Navigation
        //[JsonIgnore]
        //public Borrower Borrower { get; set; }

        //[JsonIgnore]
        //public Loan Loan { get; set; }
    }
}
