namespace LoanApplication.Dtos
{
    public class BorrowerStatementResponse
    {
        public string BorrowerName { get; set; }
        public string LoanNumber { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int DurationInMonths { get; set; }
        public List<PaymentHistoryDto> PaymentHistory { get; set; }
        public decimal OutstandingBalance { get; set; }
        public DateTimeOffset? NextPaymentDueDate { get; set; }
        public decimal NextPaymentAmount { get; set; }
    }
}
