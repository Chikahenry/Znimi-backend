namespace LoanApplication.Dtos
{
    public class CreateLoanApplicationRequest
    {
        public int BorrowerId { get; set; }
        public decimal RequestedAmount { get; set; }
        public int DurationInMonths { get; set; }
        public string Notes { get; set; }
    }
}
