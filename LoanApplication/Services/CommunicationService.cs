using LoanApplication.Data;
using LoanApplication.Domain.Enums;
using LoanApplication.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Services
{
    public interface ICommunicationService
    {
        Task SchedulePaymentReminders();
        Task SendSMS(int borrowerId, string message);
        Task SendCustomMessage(int borrowerId, int? loanId, string message, string communicationType, int sentByUserId);
    }


    public class CommunicationService : ICommunicationService
    {
        private readonly LoanManagementDbContext _context;
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<CommunicationService> _logger;

        public CommunicationService(
            LoanManagementDbContext context,
            IWhatsAppService whatsAppService,
            ILogger<CommunicationService> logger)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }


        public async Task SchedulePaymentReminders()
        {
            var today = DateTime.UtcNow.Date;

            // Get loans needing reminders
            var loansForReminders = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.Status == LoanStatus.Active ||
                           l.Status == LoanStatus.Upcoming ||
                           l.Status == LoanStatus.Overdue)
                .ToListAsync();

            foreach (var loan in loansForReminders)
            {
                var nextSchedule = loan.RepaymentSchedules
                    .Where(rs => !rs.IsPaid)
                    .OrderBy(rs => rs.DueDate)
                    .FirstOrDefault();

                if (nextSchedule != null)
                {
                    var daysUntilDue = (nextSchedule.DueDate.Date - today).Days;

                    // Send reminder at 5 days, 1 day, or if overdue
                    if (daysUntilDue == 5 || daysUntilDue == 1 || daysUntilDue == 0)
                    {
                        try
                        {
                            await _whatsAppService.SendPaymentReminder(loan.LoanId);
                            _logger.LogInformation($"Payment reminder sent for loan {loan.LoanNumber}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send reminder for loan {loan.LoanNumber}");
                        }
                    }
                    else if (daysUntilDue < 0) // Overdue
                    {
                        try
                        {
                            await _whatsAppService.SendOverdueNotification(loan.LoanId);
                            _logger.LogInformation($"Overdue notification sent for loan {loan.LoanNumber}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send overdue notification for loan {loan.LoanNumber}");
                        }
                    }
                }

                // Rate limiting
                await Task.Delay(1000);
            }
        }


        public async Task SendSMS(int borrowerId, string message)
        {
            // Integration with SMS gateway (Twilio, Termii, etc.)
            // This is a placeholder for actual SMS sending logic
            await CreateCommunication(borrowerId, null, message, "SMS", false, DateTime.UtcNow);
            await _context.SaveChangesAsync();
        }

        public async Task SendCustomMessage(int borrowerId, int? loanId, string message, string communicationType, int sentByUserId)
        {
            await CreateCommunication(borrowerId, loanId, message, communicationType, false, DateTime.UtcNow, sentByUserId);
            await _context.SaveChangesAsync();
        }

        private async Task CreateCommunication(int borrowerId, int? loanId, string message, string type, bool isAutomated, DateTime scheduledAt, int? sentByUserId = null)
        {
            var communication = new Communication
            {
                BorrowerId = borrowerId,
                LoanId = loanId,
                SentByUserId = sentByUserId,
                CommunicationType = type,
                Message = message,
                IsAutomated = isAutomated,
                ScheduledAt = scheduledAt,
                IsSent = false
            };

            _context.Communications.Add(communication);
        }
    }
}
