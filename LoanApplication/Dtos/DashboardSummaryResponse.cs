namespace LoanApplication.Dtos
{
    public class DashboardSummaryResponse
    {
        public decimal TotalLoansOutstanding { get; set; }
        public decimal InterestIncomeMonthToDate { get; set; }
        public decimal PenaltiesCollected { get; set; }
        public int ActiveLoansCount { get; set; }
        public int OverdueLoansCount { get; set; }
        public decimal AverageLoanSize { get; set; }
        public decimal AverageInterestRate { get; set; }
        public decimal TotalPrincipalDisbursedThisMonth { get; set; }
        public decimal CollectionRate { get; set; }
    }
}
