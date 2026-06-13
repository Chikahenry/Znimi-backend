using LoanApplication.Dtos;

namespace LoanApplication.Services
{
    public interface ILoanCalculatorService
    {
        LoanCalculatorResponse CalculateLoan(LoanCalculatorRequest request);
        decimal CalculateReducingBalanceInterest(decimal balance, decimal rate);
        decimal CalculateFlatRateInterest(decimal principal, decimal rate);
    }

    public class LoanCalculatorService : ILoanCalculatorService
    {
        public LoanCalculatorResponse CalculateLoan(LoanCalculatorRequest request)
        {
            var phase1Rate = request.Phase1InterestRate ?? 5.0m;
            var phase2Rate = request.Phase2InterestRate ?? 3.0m;
            var startDate = request.StartDate ?? DateTime.UtcNow;

            var installments = new List<CalculatedInstallment>();
            var remainingBalance = request.LoanAmount;
            var principalPerMonth = request.LoanAmount / request.PeriodInMonths;

            decimal totalPhase1Interest = 0;
            decimal totalPhase2Interest = 0;

            var currentDueDate = new DateTime(startDate.Year, startDate.Month, 15);
            if (currentDueDate <= startDate)
                currentDueDate = currentDueDate.AddMonths(1);

            for (int i = 1; i <= request.PeriodInMonths; i++)
            {
                string phase;
                decimal interestAmount;
                decimal appliedRate;

                // PHASE 1: First 3 months - Reducing Balance (5%)
                if (i <= 3)
                {
                    phase = "Reducing Balance";
                    appliedRate = phase1Rate;
                    interestAmount = CalculateReducingBalanceInterest(remainingBalance, phase1Rate);
                    totalPhase1Interest += interestAmount;
                }
                // PHASE 2: Month 4+ - Flat Rate on Original Principal (3%)
                else
                {
                    phase = "Flat Rate";
                    appliedRate = phase2Rate;
                    interestAmount = CalculateFlatRateInterest(request.LoanAmount, phase2Rate);
                    totalPhase2Interest += interestAmount;
                }

                var principalAmount = principalPerMonth;

                // Last installment: adjust for rounding
                if (i == request.PeriodInMonths)
                    principalAmount = remainingBalance;

                var totalPayment = principalAmount + interestAmount;
                remainingBalance -= principalAmount;

                installments.Add(new CalculatedInstallment
                {
                    InstallmentNumber = i,
                    DueDate = currentDueDate,
                    PrincipalAmount = Math.Round(principalAmount, 2),
                    InterestAmount = Math.Round(interestAmount, 2),
                    TotalPayment = Math.Round(totalPayment, 2),
                    RemainingBalance = Math.Round(remainingBalance, 2),
                    InterestPhase = phase,
                    InterestRate = appliedRate
                });

                currentDueDate = currentDueDate.AddMonths(1);
            }

            var totalInterest = totalPhase1Interest + totalPhase2Interest;
            var totalRepayment = request.LoanAmount + totalInterest;

            return new LoanCalculatorResponse
            {
                LoanAmount = request.LoanAmount,
                PeriodInMonths = request.PeriodInMonths,
                Phase1InterestRate = phase1Rate,
                Phase2InterestRate = phase2Rate,
                TotalInterest = Math.Round(totalInterest, 2),
                TotalRepayment = Math.Round(totalRepayment, 2),
                MonthlyAveragePayment = Math.Round(totalRepayment / request.PeriodInMonths, 2),
                InstallmentSchedule = installments,
                Summary = new LoanSummary
                {
                    TotalPrincipal = request.LoanAmount,
                    TotalPhase1Interest = Math.Round(totalPhase1Interest, 2),
                    TotalPhase2Interest = Math.Round(totalPhase2Interest, 2),
                    TotalInterest = Math.Round(totalInterest, 2),
                    TotalRepayment = Math.Round(totalRepayment, 2),
                    EffectiveInterestRate = Math.Round((totalInterest / request.LoanAmount) * 100, 2),
                    Phase1Months = Math.Min(3, request.PeriodInMonths),
                    Phase2Months = Math.Max(0, request.PeriodInMonths - 3)
                }
            };
        }

        public decimal CalculateReducingBalanceInterest(decimal balance, decimal rate)
        {
            return balance * (rate / 100m);
        }

        public decimal CalculateFlatRateInterest(decimal principal, decimal rate)
        {
            return principal * (rate / 100m);
        }
    }
}
