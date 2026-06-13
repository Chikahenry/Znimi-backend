using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class WhatsAppTemplate
    {
        [Key]
        public int TemplateId { get; set; }

        [Required, MaxLength(100)]
        public string TemplateName { get; set; }

        [Required]
        public string TemplateBody { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
