using LoanApplication.Domain.Enums;

namespace LoanApplication.Dtos
{
    public class RecordPaymentRequest
    {
        public int LoanId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Notes { get; set; }
    }
}
