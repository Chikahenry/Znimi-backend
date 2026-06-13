namespace LoanApplication.Dtos
{
    public class ApproveLoanRequest
    {
        public int LoanId { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public int? ModifiedDuration { get; set; }
        public decimal? CustomPhase1Rate { get; set; }
        public decimal? CustomPhase2Rate { get; set; }
    }
}
