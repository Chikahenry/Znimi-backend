using LoanApplication.Data;
using LoanApplication.Domain.Models;
using LoanApplication.Dtos;
using Microsoft.EntityFrameworkCore;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LoanApplication.Services
{
    public interface IWhatsAppService
    {
        Task<WhatsAppMessageResponse> SendMessage(string phoneNumber, string message, string messageType = null);
        Task<WhatsAppMessageResponse> SendTemplateMessage(string phoneNumber, string templateName, Dictionary<string, string> parameters);
        Task<bool> SendPaymentReminder(int loanId);
        Task<bool> SendPaymentConfirmation(int paymentId);
        Task<bool> SendLoanApprovalNotification(int loanId);
        Task<bool> SendLoanDisbursementNotification(int loanId);
        Task<bool> SendOverdueNotification(int loanId);
        Task<bool> SendStatementToWhatsApp(int borrowerId, int loanId);
        Task<int> SendBulkMessages(BulkWhatsAppRequest request);
        Task<List<WhatsAppMessage>> GetMessageHistory(int borrowerId);
        Task UpdateMessageStatus(string messageSid, string status);
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly LoanManagementDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _twilioWhatsAppNumber;

        public WhatsAppService(
            LoanManagementDbContext context,
            IConfiguration configuration,
            ILogger<WhatsAppService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            _twilioAccountSid = configuration["Twilio:AccountSid"];
            _twilioAuthToken = configuration["Twilio:AuthToken"];
            _twilioWhatsAppNumber = configuration["Twilio:WhatsAppNumber"]; // whatsapp:+14155238886 (Twilio Sandbox)

            TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);
        }

        public async Task<WhatsAppMessageResponse> SendMessage(string phoneNumber, string message, string messageType = null)
        {
            try
            {
                // Format phone number for WhatsApp (must include country code)
                var formattedNumber = FormatPhoneNumber(phoneNumber);

                _logger.LogInformation($"Sending WhatsApp message to {formattedNumber}");

                // Send via Twilio
                var twilioMessage = await MessageResource.CreateAsync(
                    from: new PhoneNumber(_twilioWhatsAppNumber),
                    to: new PhoneNumber($"whatsapp:{formattedNumber}"),
                    body: message
                );

                // Save to database
                var whatsappMessage = new WhatsAppMessage
                {
                    RecipientPhoneNumber = phoneNumber,
                    MessageBody = message,
                    MessageType = messageType ?? "General",
                    MessageSid = twilioMessage.Sid,
                    Status = twilioMessage.Status.ToString(),
                    IsSent = true,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = ""
                };

                _context.WhatsAppMessages.Add(whatsappMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"WhatsApp message sent successfully. SID: {twilioMessage.Sid}");

                return new WhatsAppMessageResponse
                {
                    Success = true,
                    MessageSid = twilioMessage.Sid,
                    Status = twilioMessage.Status.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending WhatsApp message to {phoneNumber}");

                // Save failed message
                var failedMessage = new WhatsAppMessage
                {
                    RecipientPhoneNumber = phoneNumber,
                    MessageBody = message,
                    MessageType = messageType ?? "General",
                    Status = "failed",
                    IsSent = false,
                    ErrorMessage = ex.Message
                };

                _context.WhatsAppMessages.Add(failedMessage);
                await _context.SaveChangesAsync();

                return new WhatsAppMessageResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<WhatsAppMessageResponse> SendTemplateMessage(string phoneNumber, string templateName, Dictionary<string, string> parameters)
        {
            var template = await _context.WhatsAppTemplates
                .FirstOrDefaultAsync(t => t.TemplateName == templateName && t.IsActive);

            if (template == null)
            {
                return new WhatsAppMessageResponse
                {
                    Success = false,
                    ErrorMessage = "Template not found"
                };
            }

            // Replace parameters in template
            var message = template.TemplateBody;
            foreach (var param in parameters)
            {
                message = message.Replace($"{{{param.Key}}}", param.Value);
            }

            return await SendMessage(phoneNumber, message, templateName);
        }

        public async Task<bool> SendPaymentReminder(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null) return false;

            var nextSchedule = loan.RepaymentSchedules
                .Where(rs => !rs.IsPaid)
                .OrderBy(rs => rs.DueDate)
                .FirstOrDefault();

            if (nextSchedule == null) return false;

            var daysUntilDue = (nextSchedule.DueDate.Date - DateTime.UtcNow.Date).Days;

            string message;
            if (daysUntilDue == 5)
            {
                message = $"🔔 Hello {loan.Borrower.FullName},\n\n" +
                         $"This is a friendly reminder that your loan payment of *₦{nextSchedule.TotalAmount:N2}* is due on *{nextSchedule.DueDate:MMM dd, yyyy}*.\n\n" +
                         $"Loan: {loan.LoanNumber}\n" +
                         $"Amount: ₦{nextSchedule.TotalAmount:N2}\n" +
                         $"Due Date: {nextSchedule.DueDate:MMM dd, yyyy}\n\n" +
                         $"Thank you for your prompt payment! 🙏";
            }
            else if (daysUntilDue == 1)
            {
                message = $"⏰ Hello {loan.Borrower.FullName},\n\n" +
                         $"Your loan payment is due *TOMORROW*!\n\n" +
                         $"Amount: ₦{nextSchedule.TotalAmount:N2}\n" +
                         $"Due Date: {nextSchedule.DueDate:MMM dd, yyyy}\n\n" +
                         $"Please ensure payment is made to avoid penalties.\n\n" +
                         $"Need help? Reply to this message.";
            }
            else if (daysUntilDue == 0)
            {
                message = $"📅 Hello {loan.Borrower.FullName},\n\n" +
                         $"Your loan payment is due *TODAY*!\n\n" +
                         $"Amount: ₦{nextSchedule.TotalAmount:N2}\n\n" +
                         $"Please make your payment today to stay current on your loan.";
            }
            else
            {
                return false;
            }

            var result = await SendMessage(loan.Borrower.PhoneNumber, message, "PaymentReminder");

            if (result.Success)
            {
                // Update message record with borrower and loan info
                var messageRecord = await _context.WhatsAppMessages
                    .FirstOrDefaultAsync(m => m.MessageSid == result.MessageSid);

                if (messageRecord != null)
                {
                    messageRecord.BorrowerId = loan.BorrowerId;
                    messageRecord.LoanId = loanId;
                    await _context.SaveChangesAsync();
                }
            }

            return result.Success;
        }

        public async Task<bool> SendPaymentConfirmation(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Loan)
                    .ThenInclude(l => l.Borrower)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null) return false;

            var message = $"✅ *Payment Received*\n\n" +
                         $"Hello {payment.Loan.Borrower.FullName},\n\n" +
                         $"Thank you! Your payment has been received and processed.\n\n" +
                         $"*Payment Details:*\n" +
                         $"Receipt #: {payment.PaymentNumber}\n" +
                         $"Amount: ₦{payment.Amount:N2}\n" +
                         $"Date: {payment.PaymentDate:MMM dd, yyyy}\n\n" +
                         $"*Payment Allocation:*\n" +
                         $"• Principal: ₦{payment.PrincipalPortion:N2}\n" +
                         $"• Interest: ₦{payment.InterestPortion:N2}\n" +
                         $"• Penalties: ₦{payment.PenaltyPortion:N2}\n\n" +
                         $"Outstanding Balance: ₦{payment.Loan.OutstandingPrincipal:N2}\n\n" +
                         $"Thank you for your business! 🙏";

            var result = await SendMessage(payment.Loan.Borrower.PhoneNumber, message, "PaymentConfirmation");

            if (result.Success)
            {
                var messageRecord = await _context.WhatsAppMessages
                    .FirstOrDefaultAsync(m => m.MessageSid == result.MessageSid);

                if (messageRecord != null)
                {
                    messageRecord.BorrowerId = payment.Loan.BorrowerId;
                    messageRecord.LoanId = payment.LoanId;
                    await _context.SaveChangesAsync();
                }
            }

            return result.Success;
        }

        public async Task<bool> SendLoanApprovalNotification(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null) return false;

            var message = $"🎉 *Loan Approved!*\n\n" +
                         $"Hello {loan.Borrower.FullName},\n\n" +
                         $"Great news! Your loan application has been *APPROVED*!\n\n" +
                         $"*Loan Details:*\n" +
                         $"Loan #: {loan.LoanNumber}\n" +
                         $"Amount: ₦{loan.ApprovedAmount:N2}\n" +
                         $"Duration: {loan.DurationInMonths} months\n" +
                         $"Interest Rate: {loan.Phase1InterestRate}% (first 3 months), {loan.Phase2InterestRate}% thereafter\n\n" +
                         $"Next steps: Your funds will be disbursed shortly. You'll receive another message once disbursement is complete.\n\n" +
                         $"Questions? Reply to this message! 📱";

            var result = await SendMessage(loan.Borrower.PhoneNumber, message, "LoanApproval");

            if (result.Success)
            {
                var messageRecord = await _context.WhatsAppMessages
                    .FirstOrDefaultAsync(m => m.MessageSid == result.MessageSid);

                if (messageRecord != null)
                {
                    messageRecord.BorrowerId = loan.BorrowerId;
                    messageRecord.LoanId = loanId;
                    await _context.SaveChangesAsync();
                }
            }

            return result.Success;
        }

        public async Task<bool> SendLoanDisbursementNotification(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null || !loan.DisbursementDate.HasValue) return false;

            var message = $"💰 *Funds Disbursed!*\n\n" +
                         $"Hello {loan.Borrower.FullName},\n\n" +
                         $"Your loan has been disbursed!\n\n" +
                         $"*Details:*\n" +
                         $"Amount: ₦{loan.ApprovedAmount:N2}\n" +
                         $"Method: {loan.DisbursementMethod}\n" +
                         $"Date: {loan.DisbursementDate}\n\n" +
                         $"*First Payment:*\n" +
                         $"Due Date: {loan.FirstPaymentDueDate}\n\n" +
                         $"Thank you for choosing us! We look forward to serving you. 🙏";

            var result = await SendMessage(loan.Borrower.PhoneNumber, message, "LoanDisbursement");

            if (result.Success)
            {
                var messageRecord = await _context.WhatsAppMessages
                    .FirstOrDefaultAsync(m => m.MessageSid == result.MessageSid);

                if (messageRecord != null)
                {
                    messageRecord.BorrowerId = loan.BorrowerId;
                    messageRecord.LoanId = loanId;
                    await _context.SaveChangesAsync();
                }
            }

            return result.Success;
        }

        public async Task<bool> SendOverdueNotification(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null) return false;

            var overdueSchedule = loan.RepaymentSchedules
                .Where(rs => !rs.IsPaid && rs.DueDate < DateTime.UtcNow)
                .OrderBy(rs => rs.DueDate)
                .FirstOrDefault();

            if (overdueSchedule == null) return false;

            var daysOverdue = (DateTime.UtcNow.Date - overdueSchedule.DueDate.Date).Days;

            var message = $"⚠️ *Payment Overdue*\n\n" +
                         $"Hello {loan.Borrower.FullName},\n\n" +
                         $"Your loan payment is now *{daysOverdue} days overdue*.\n\n" +
                         $"*Overdue Payment:*\n" +
                         $"Amount Due: ₦{overdueSchedule.TotalAmount:N2}\n" +
                         $"Original Due Date: {overdueSchedule.DueDate}\n" +
                         $"Penalties: ₦{loan.TotalPenaltiesAccrued:N2}\n\n" +
                         $"*Total Amount Due Now: ₦{overdueSchedule.TotalAmount + loan.TotalPenaltiesAccrued:N2}*\n\n" +
                         $"Please contact us immediately to arrange payment and avoid further penalties.\n\n" +
                         $"📞 Need help? Reply to this message or call us.";

            var result = await SendMessage(loan.Borrower.PhoneNumber, message, "OverdueNotification");

            if (result.Success)
            {
                var messageRecord = await _context.WhatsAppMessages
                    .FirstOrDefaultAsync(m => m.MessageSid == result.MessageSid);

                if (messageRecord != null)
                {
                    messageRecord.BorrowerId = loan.BorrowerId;
                    messageRecord.LoanId = loanId;
                    await _context.SaveChangesAsync();
                }
            }

            return result.Success;
        }

        public async Task<bool> SendStatementToWhatsApp(int borrowerId, int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Payments)
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.LoanId == loanId && l.BorrowerId == borrowerId);

            if (loan == null) return false;

            var paidCount = loan.RepaymentSchedules.Count(rs => rs.IsPaid);
            var totalPayments = loan.Payments.Sum(p => p.Amount);

            var message = $"📊 *Loan Statement*\n\n" +
                         $"Hello {loan.Borrower.FullName},\n\n" +
                         $"*Loan Summary:*\n" +
                         $"Loan #: {loan.LoanNumber}\n" +
                         $"Original Amount: ₦{loan.ApprovedAmount:N2}\n" +
                         $"Disbursement Date: {loan.DisbursementDate}\n\n" +
                         $"*Payment Progress:*\n" +
                         $"Installments Paid: {paidCount}/{loan.DurationInMonths}\n" +
                         $"Total Paid: ₦{totalPayments:N2}\n" +
                         $"Outstanding Balance: ₦{loan.OutstandingPrincipal:N2}\n" +
                         $"Status: {loan.Status}\n\n" +
                         $"For detailed statement, please contact us.\n\n" +
                         $"Thank you! 🙏";

            var result = await SendMessage(loan.Borrower.PhoneNumber, message, "Statement");

            if (result.Success)
            {
                var messageRecord = await _context.WhatsAppMessages
                    .FirstOrDefaultAsync(m => m.MessageSid == result.MessageSid);

                if (messageRecord != null)
                {
                    messageRecord.BorrowerId = borrowerId;
                    messageRecord.LoanId = loanId;
                    await _context.SaveChangesAsync();
                }
            }

            return result.Success;
        }

        public async Task<int> SendBulkMessages(BulkWhatsAppRequest request)
        {
            var successCount = 0;

            foreach (var borrowerId in request.BorrowerIds)
            {
                var borrower = await _context.Borrowers.FindAsync(borrowerId);
                if (borrower == null) continue;

                var result = await SendTemplateMessage(
                    borrower.PhoneNumber,
                    request.TemplateName,
                    request.Parameters ?? new Dictionary<string, string>()
                );

                if (result.Success)
                {
                    successCount++;

                    // Update message record
                    var messageRecord = await _context.WhatsAppMessages
                        .FirstOrDefaultAsync(m => m.MessageSid == result.MessageSid);

                    if (messageRecord != null)
                    {
                        messageRecord.BorrowerId = borrowerId;
                        await _context.SaveChangesAsync();
                    }
                }

                // Rate limiting - wait 1 second between messages
                await Task.Delay(1000);
            }

            return successCount;
        }

        public async Task<List<WhatsAppMessage>> GetMessageHistory(int borrowerId)
        {
            return await _context.WhatsAppMessages
                .Where(m => m.BorrowerId == borrowerId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task UpdateMessageStatus(string messageSid, string status)
        {
            var message = await _context.WhatsAppMessages
                .FirstOrDefaultAsync(m => m.MessageSid == messageSid);

            if (message != null)
            {
                message.Status = status;

                if (status == "delivered")
                    message.DeliveredAt = DateTime.UtcNow;
                else if (status == "read")
                    message.ReadAt = DateTime.UtcNow;
                else if (status == "failed")
                    message.IsSent = false;

                await _context.SaveChangesAsync();
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove all non-numeric characters
            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // If doesn't start with country code, add Nigeria's (+234)
            if (!cleaned.StartsWith("234"))
            {
                if (cleaned.StartsWith("0"))
                    cleaned = "234" + cleaned.Substring(1);
                else
                    cleaned = "234" + cleaned;
            }

            return "+" + cleaned;
        }
    }
}
