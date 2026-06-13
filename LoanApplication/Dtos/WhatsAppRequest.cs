using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Dtos
{
    public class SendWhatsAppRequest
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Message { get; set; }

        public string MessageType { get; set; }
    }

    public class WhatsAppMessageResponse
    {
        public bool Success { get; set; }
        public string MessageSid { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class BulkWhatsAppRequest
    {
        public List<int> BorrowerIds { get; set; }
        public string TemplateName { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}
