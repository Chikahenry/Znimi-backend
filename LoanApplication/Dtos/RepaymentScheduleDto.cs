namespace LoanApplication.Dtos
{
    public class RepaymentScheduleDto
    {
        public int InstallmentNumber { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BalanceAfterPayment { get; set; }
        public string InterestPhase { get; set; }
        public bool IsPaid { get; set; }
        public decimal AmountPaid { get; set; }
    }
}
