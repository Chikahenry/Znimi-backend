using Hangfire;
using Microsoft.EntityFrameworkCore;
using LoanApplication.Data;
using LoanApplication.Domain.Enums;
using LoanApplication.Services;
using LoanApplication.Domain.Models;

namespace LoanApplication.BackgroundJobs
{

    // Background Job Service
    public interface IBackgroundJobService
    {
        Task UpdateLoanStatusesJob();
        Task SendPaymentRemindersJob();
        Task CalculatePenaltiesJob();
        Task UpdateBorrowerCreditScoresJob();
        Task GenerateDailyReportsJob();
        Task ArchiveClosedLoansJob();
    }

    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task UpdateLoanStatusesJob()
        {
            using var scope = _serviceProvider.CreateScope();
            var loanService = scope.ServiceProvider.GetRequiredService<ILoanService>();

            _logger.LogInformation("Starting loan status update job...");

            try
            {
                await loanService.UpdateLoanStatuses();
                _logger.LogInformation("Loan status update completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loan statuses");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task SendPaymentRemindersJob()
        {
            using var scope = _serviceProvider.CreateScope();
            var communicationService = scope.ServiceProvider.GetRequiredService<ICommunicationService>();

            _logger.LogInformation("Starting payment reminders job...");

            try
            {
                await communicationService.SchedulePaymentReminders();
                _logger.LogInformation("Payment reminders sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment reminders");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task CalculatePenaltiesJob()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LoanManagementDbContext>();

            _logger.LogInformation("Starting penalty calculation job...");

            try
            {
                var today = DateTime.UtcNow.Date;

                var overdueLoans = await context.Loans
                    .Include(l => l.RepaymentSchedules)
                    .Where(l => l.Status == LoanStatus.Overdue || l.Status == LoanStatus.AtRisk)
                    .ToListAsync();

                foreach (var loan in overdueLoans)
                {
                    var nextUnpaidSchedule = loan.RepaymentSchedules
                        .Where(rs => !rs.IsPaid)
                        .OrderBy(rs => rs.DueDate)
                        .FirstOrDefault();

                    if (nextUnpaidSchedule != null)
                    {
                        var daysOverdue = (today - nextUnpaidSchedule.DueDate.Date).Days;
                        var penaltyDays = daysOverdue - loan.GracePeriodDays;

                        if (penaltyDays > 0)
                        {
                            var dailyPenalty = loan.DailyPenaltyAmount > 0
                                ? loan.DailyPenaltyAmount
                                : (nextUnpaidSchedule.TotalAmount * loan.PenaltyPercentage / 100m);

                            var newPenalty = dailyPenalty; // Add one day's penalty
                            loan.TotalPenaltiesAccrued += newPenalty;
                        }
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"Penalties calculated for {overdueLoans.Count} loans");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating penalties");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task UpdateBorrowerCreditScoresJob()
        {
            using var scope = _serviceProvider.CreateScope();
            var borrowerService = scope.ServiceProvider.GetRequiredService<IBorrowerService>();
            var context = scope.ServiceProvider.GetRequiredService<LoanManagementDbContext>();

            _logger.LogInformation("Starting credit score update job...");

            try
            {
                var borrowers = await context.Borrowers
                    .Where(b => b.IsActive)
                    .ToListAsync();

                foreach (var borrower in borrowers)
                {
                    await borrowerService.UpdateBorrowerCreditScore(borrower.BorrowerId);
                }

                _logger.LogInformation($"Credit scores updated for {borrowers.Count} borrowers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credit scores");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task GenerateDailyReportsJob()
        {
            using var scope = _serviceProvider.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
            var context = scope.ServiceProvider.GetRequiredService<LoanManagementDbContext>();

            _logger.LogInformation("Starting daily reports generation...");

            try
            {
                var summary = await analyticsService.GetDashboardSummary();
                var portfolioHealth = await analyticsService.GetPortfolioHealth();

                // Store daily snapshot
                var snapshot = new DailySnapshot
                {
                    SnapshotDate = DateTime.UtcNow.Date,
                    TotalLoansOutstanding = summary.TotalLoansOutstanding,
                    ActiveLoansCount = summary.ActiveLoansCount,
                    OverdueLoansCount = summary.OverdueLoansCount,
                    InterestIncomeMonthToDate = summary.InterestIncomeMonthToDate,
                    PAR7Percentage = portfolioHealth.PAR7Percentage,
                    PAR30Percentage = portfolioHealth.PAR30Percentage,
                    CollectionRate = summary.CollectionRate
                };

                context.DailySnapshots.Add(snapshot);
                await context.SaveChangesAsync();

                _logger.LogInformation("Daily reports generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily reports");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task ArchiveClosedLoansJob()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LoanManagementDbContext>();

            _logger.LogInformation("Starting loan archival job...");

            try
            {
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

                var loansToArchive = await context.Loans
                    .Where(l => l.Status == LoanStatus.Closed &&
                               l.ClosedDate.HasValue &&
                               l.ClosedDate.Value < sixMonthsAgo)
                    .Take(100) // Process in batches
                    .ToListAsync();

                // Could move to archive table or just flag them
                foreach (var loan in loansToArchive)
                {
                    loan.Notes = $"[ARCHIVED] {loan.Notes}";
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"Archived {loansToArchive.Count} closed loans");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving loans");
                throw;
            }
        }
    }
}
