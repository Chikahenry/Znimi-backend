using LoanApplication.Data;
using LoanApplication.Domain.Enums;
using LoanApplication.Domain.Models;
using LoanApplication.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardSummaryResponse> GetDashboardSummary();
        Task<PortfolioHealthResponse> GetPortfolioHealth();
        Task<CashFlowProjectionResponse> GetCashFlowProjection(int days = 30);
        Task<Dictionary<string, decimal>> GetRevenueReport(DateTime fromDate, DateTime toDate);
        Task<List<Borrower>> GetTopPerformingBorrowers(int count = 10);
        Task<List<Borrower>> GetHighRiskBorrowers(int count = 10);
    }


    public class AnalyticsService : IAnalyticsService
    {
        private readonly LoanManagementDbContext _context;

        public AnalyticsService(LoanManagementDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryResponse> GetDashboardSummary()
        {
            var monthStart = new DateTimeOffset(
                            DateTime.UtcNow.Year,
                            DateTime.UtcNow.Month,
                            1,
                            0, 0, 0,
                            TimeSpan.Zero // CRITICAL
                        );

            var activeLoans = await _context.Loans
                .Where(l => l.Status != LoanStatus.Closed && l.Status != LoanStatus.Defaulted)
                .ToListAsync();

            var paymentsThisMonth = await _context.Payments
                .Where(p => p.PaymentDate >= monthStart)
                .ToListAsync();

            var expectedPaymentsThisMonth = await _context.RepaymentSchedules
                .Where(rs => rs.DueDate >= monthStart && rs.DueDate < monthStart.AddMonths(1))
                .SumAsync(rs => rs.TotalAmount);

            var actualPaymentsThisMonth = paymentsThisMonth.Sum(p => p.Amount);

            return new DashboardSummaryResponse
            {
                TotalLoansOutstanding = activeLoans.Sum(l => l.OutstandingPrincipal),
                InterestIncomeMonthToDate = paymentsThisMonth.Sum(p => p.InterestPortion),
                PenaltiesCollected = paymentsThisMonth.Sum(p => p.PenaltyPortion),
                ActiveLoansCount = activeLoans.Count(l => l.Status == LoanStatus.Active),
                OverdueLoansCount = activeLoans.Count(l => l.Status == LoanStatus.Overdue || l.Status == LoanStatus.AtRisk),
                AverageLoanSize = activeLoans.Any() ? activeLoans.Average(l => l.ApprovedAmount) : 0,
                AverageInterestRate = activeLoans.Any() ? activeLoans.Average(l => l.Phase1InterestRate) : 0,
                TotalPrincipalDisbursedThisMonth = await _context.Loans
                    .Where(l => l.DisbursementDate >= monthStart && l.DisbursementDate < monthStart.AddMonths(1))
                    .SumAsync(l => l.ApprovedAmount),
                CollectionRate = expectedPaymentsThisMonth > 0
                    ? (actualPaymentsThisMonth / expectedPaymentsThisMonth * 100)
                    : 100
            };
        }

        public async Task<PortfolioHealthResponse> GetPortfolioHealth()
        {
            var today = DateTime.UtcNow.Date;
            var totalPortfolioValue = await _context.Loans
                .Where(l => l.Status != LoanStatus.Closed)
                .SumAsync(l => l.OutstandingPrincipal);

            var par7Value = await _context.Loans
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.RepaymentSchedules.Any(rs =>
                    !rs.IsPaid &&
                    rs.DueDate < today.AddDays(-7)))
                .SumAsync(l => l.OutstandingPrincipal);

            var par30Value = await _context.Loans
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.RepaymentSchedules.Any(rs =>
                    !rs.IsPaid &&
                    rs.DueDate < today.AddDays(-30)))
                .SumAsync(l => l.OutstandingPrincipal);

            var loansByStatus = await _context.Loans
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Get trend data for last 6 months
            var trendData = new List<MonthlyTrendData>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                // Calculate PAR for that month (simplified)
                trendData.Add(new MonthlyTrendData
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    PAR7 = 0, // Simplified - would need historical data
                    PAR30 = 0
                });
            }

            return new PortfolioHealthResponse
            {
                PAR7Percentage = totalPortfolioValue > 0 ? (par7Value / totalPortfolioValue * 100) : 0,
                PAR30Percentage = totalPortfolioValue > 0 ? (par30Value / totalPortfolioValue * 100) : 0,
                LoansByStatus = loansByStatus,
                LoansBySizeBracket = await GetLoansBySizeBracket(),
                TrendData = trendData
            };
        }

        public async Task<CashFlowProjectionResponse> GetCashFlowProjection(int days = 30)
        {
            var today = DateTime.UtcNow.Date;
            var endDate = today.AddDays(days);

            var schedules = await _context.RepaymentSchedules
                .Include(rs => rs.Loan)
                .Where(rs => !rs.IsPaid && rs.DueDate >= today && rs.DueDate <= endDate)
                .ToListAsync();

            var weeklyProjections = new List<WeeklyProjection>();
            var currentDate = today;

            while (currentDate <= endDate)
            {
                var weekEnd = currentDate.AddDays(6);
                var weekSchedules = schedules
                    .Where(s => s.DueDate >= currentDate && s.DueDate <= weekEnd)
                    .ToList();

                var dailyPayments = weekSchedules
                    .GroupBy(s => s.DueDate.Date)
                    .Select(g => new DailyPayment
                    {
                        Date = g.Key,
                        ExpectedAmount = g.Sum(s => s.TotalAmount),
                        LoanCount = g.Count(),
                        Status = g.Key < today ? "Overdue" : (g.Key == today ? "Due" : "Upcoming")
                    })
                    .ToList();

                weeklyProjections.Add(new WeeklyProjection
                {
                    WeekRange = $"{currentDate:MMM dd} - {weekEnd:MMM dd}",
                    DailyPayments = dailyPayments,
                    TotalExpected = weekSchedules.Sum(s => s.TotalAmount)
                });

                currentDate = weekEnd.AddDays(1);
            }

            return new CashFlowProjectionResponse
            {
                WeeklyProjections = weeklyProjections
            };
        }

        public async Task<Dictionary<string, decimal>> GetRevenueReport(DateTime fromDate, DateTime toDate)
        {
            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalRevenue", payments.Sum(p => p.Amount) },
                { "InterestIncome", payments.Sum(p => p.InterestPortion) },
                { "PenaltyIncome", payments.Sum(p => p.PenaltyPortion) },
                { "PrincipalRecovered", payments.Sum(p => p.PrincipalPortion) }
            };
        }

        public async Task<List<Borrower>> GetTopPerformingBorrowers(int count = 10)
        {
            return await _context.Borrowers
                .OrderByDescending(b => b.OnTimePaymentPercentage)
                .ThenByDescending(b => b.TotalLoansCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Borrower>> GetHighRiskBorrowers(int count = 10)
        {
            return await _context.Borrowers
                .Where(b => b.InternalCreditScore == CreditScore.C || b.InternalCreditScore == CreditScore.D)
                .OrderBy(b => b.OnTimePaymentPercentage)
                .ThenByDescending(b => b.DefaultedLoansCount)
                .Take(count)
                .ToListAsync();
        }

        private async Task<Dictionary<string, int>> GetLoansBySizeBracket()
        {
            var loans = await _context.Loans
                .Where(l => l.Status != LoanStatus.Closed)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                { "0-50k", loans.Count(l => l.ApprovedAmount < 50000) },
                { "50k-100k", loans.Count(l => l.ApprovedAmount >= 50000 && l.ApprovedAmount < 100000) },
                { "100k-200k", loans.Count(l => l.ApprovedAmount >= 100000 && l.ApprovedAmount < 200000) },
                { "200k+", loans.Count(l => l.ApprovedAmount >= 200000) }
            };
        }
    }
}
