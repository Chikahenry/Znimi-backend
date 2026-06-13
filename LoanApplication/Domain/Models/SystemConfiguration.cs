using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class SystemConfiguration
    {
        [Key]
        public int ConfigId { get; set; }

        [Required, MaxLength(100)]
        public string ConfigKey { get; set; }

        [Required]
        public string ConfigValue { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
