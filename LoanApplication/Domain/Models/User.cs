using LoanApplication.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoanApplication.Domain.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<Loan> CreatedLoans { get; set; }
        [JsonIgnore]
        public ICollection<Payment> RecordedPayments { get; set; }
        [JsonIgnore]
        public ICollection<AuditLog> AuditLogs { get; set; }
    }
}
