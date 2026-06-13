using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Dtos
{
    public class LoanCalculatorRequest
    {
        [Required]
        [Range(10000, 10000000)]
        public decimal LoanAmount { get; set; }

        [Required]
        [Range(1, 60)]
        public int PeriodInMonths { get; set; }

        [Range(0.1, 50)]
        public decimal? Phase1InterestRate { get; set; } = 5.0m; // Default 5%

        [Range(0.1, 50)]
        public decimal? Phase2InterestRate { get; set; } = 3.0m; // Default 3%

        public DateTime? StartDate { get; set; }
    }

    public class LoanCalculatorResponse
    {
        public decimal LoanAmount { get; set; }
        public int PeriodInMonths { get; set; }
        public decimal Phase1InterestRate { get; set; }
        public decimal Phase2InterestRate { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalRepayment { get; set; }
        public decimal MonthlyAveragePayment { get; set; }
        public List<CalculatedInstallment> InstallmentSchedule { get; set; }
        public LoanSummary Summary { get; set; }
    }

    public class CalculatedInstallment
    {
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal RemainingBalance { get; set; }
        public string InterestPhase { get; set; }
        public decimal InterestRate { get; set; }
    }

    public class LoanSummary
    {
        public decimal TotalPrincipal { get; set; }
        public decimal TotalPhase1Interest { get; set; }
        public decimal TotalPhase2Interest { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalRepayment { get; set; }
        public decimal EffectiveInterestRate { get; set; }
        public int Phase1Months { get; set; }
        public int Phase2Months { get; set; }
    }
}
