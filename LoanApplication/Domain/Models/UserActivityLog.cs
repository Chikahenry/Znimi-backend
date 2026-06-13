using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class UserActivityLog
    {
        [Key]
        public int ActivityId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string ActivityType { get; set; } // Login, Logout, ViewLoan, CreateLoan, etc.

        [Required, MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
