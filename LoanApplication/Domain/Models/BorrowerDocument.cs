using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class BorrowerDocument
    {
        [Key]
        public int DocumentId { get; set; }

        public int BorrowerId { get; set; }

        [Required, MaxLength(100)]
        public string DocumentType { get; set; } // ID Card, Passport, Business Permit, etc.

        [Required, MaxLength(500)]
        public string FilePath { get; set; }

        [MaxLength(100)]
        public string FileName { get; set; }

        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation
        [ForeignKey("BorrowerId")]
        public Borrower Borrower { get; set; }
    }
}
