namespace LoanApplication.Dtos
{
    public class PaymentHistoryDto
    {
        public string PaymentNumber { get; set; }
        public DateTimeOffset PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PenaltyPortion { get; set; }
        public decimal InterestPortion { get; set; }
        public decimal PrincipalPortion { get; set; }
        public string PaymentMethod { get; set; }
        public string RecordedBy { get; set; }
    }
}
