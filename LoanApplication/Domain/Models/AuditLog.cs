using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoanApplication.Domain.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; }

        [MaxLength(100)]
        public string EntityType { get; set; }

        public int? EntityId { get; set; }

        public string OldValues { get; set; } // JSON

        public string NewValues { get; set; } // JSON

        [MaxLength(500)]
        public string Reason { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        // Navigation
        [JsonIgnore]
        public User User { get; set; }
    }
}
