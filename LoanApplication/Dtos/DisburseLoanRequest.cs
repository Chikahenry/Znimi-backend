using LoanApplication.Domain.Enums;

namespace LoanApplication.Dtos
{
    public class DisburseLoanRequest
    {
        public int LoanId { get; set; }
        public DateTime DisbursementDate { get; set; }
        public PaymentMethod DisbursementMethod { get; set; }
    }
}
