using LoanApplication.Data;
using LoanApplication.Domain.Models;
using LoanApplication.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Services
{
    public interface IPaymentService
    {
        Task<Payment> RecordPayment(RecordPaymentRequest request, int recordedByUserId);
        Task<List<Payment>> GetPaymentsWithFilters(PaymentFilterRequest filter);
        Task<string> GenerateReceipt(int paymentId);
    }


    public class PaymentService : IPaymentService
    {
        private readonly LoanManagementDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IWhatsAppService _whatsAppService;

        public PaymentService(LoanManagementDbContext context,
            IWhatsAppService whatsAppService, IAuditService auditService)
        {
            _whatsAppService = whatsAppService;
            _context = context;
            _auditService = auditService;
        }

        public async Task<Payment> RecordPayment(RecordPaymentRequest request, int recordedByUserId)
        {
            var loan = await _context.Loans
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.LoanId == request.LoanId);

            if (loan == null)
                throw new Exception("Loan not found");

            var paymentNumber = await GeneratePaymentNumber();
            var remainingAmount = request.Amount;

            // Allocate payment: Penalties -> Interest -> Principal
            var penaltyPortion = Math.Min(remainingAmount, loan.TotalPenaltiesAccrued);
            remainingAmount -= penaltyPortion;
            loan.TotalPenaltiesAccrued -= penaltyPortion;

            // Get unpaid schedules
            var unpaidSchedules = loan.RepaymentSchedules
                .Where(rs => !rs.IsPaid)
                .OrderBy(rs => rs.DueDate)
                .ToList();

            decimal interestPortion = 0;
            decimal principalPortion = 0;

            foreach (var schedule in unpaidSchedules)
            {
                if (remainingAmount <= 0) break;

                var interestDue = schedule.InterestAmount - (schedule.AmountPaid > schedule.PrincipalAmount
                    ? schedule.AmountPaid - schedule.PrincipalAmount
                    : 0);

                var interestPayment = Math.Min(remainingAmount, interestDue);
                interestPortion += interestPayment;
                remainingAmount -= interestPayment;

                if (remainingAmount > 0)
                {
                    var principalDue = schedule.PrincipalAmount - Math.Min(schedule.AmountPaid, schedule.PrincipalAmount);
                    var principalPayment = Math.Min(remainingAmount, principalDue);
                    principalPortion += principalPayment;
                    remainingAmount -= principalPayment;

                    schedule.AmountPaid += interestPayment + principalPayment;

                    if (schedule.AmountPaid >= schedule.TotalAmount)
                    {
                        schedule.IsPaid = true;
                        schedule.PaidDate = request.PaymentDate.ToUniversalTime();
                    }
                }
            }

            loan.TotalInterestAccrued += interestPortion;
            loan.OutstandingPrincipal -= principalPortion;
            loan.LastPaymentDate = request.PaymentDate.ToUniversalTime();

            var payment = new Payment
            {
                PaymentNumber = paymentNumber,
                LoanId = request.LoanId,
                RecordedByUserId = recordedByUserId,
                Amount = request.Amount,
                PenaltyPortion = penaltyPortion,
                InterestPortion = interestPortion,
                PrincipalPortion = principalPortion,
                PaymentMethod = request.PaymentMethod,
                PaymentDate = request.PaymentDate.ToUniversalTime(),
                Notes = request.Notes,
                ReceiptNumber = $"RCP-{paymentNumber}"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            // Send WhatsApp confirmation
            await _whatsAppService.SendPaymentConfirmation(payment.PaymentId);
            await _auditService.LogAction(recordedByUserId, "RecordPayment", "Payment", payment.PaymentId, "null", payment);

            return payment;
        }

        public async Task<List<Payment>> GetPaymentsWithFilters(PaymentFilterRequest filter)
        {
            var query = _context.Payments
                .Include(p => p.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(p => p.RecordedBy)
                .AsQueryable();

            if (filter.LoanId.HasValue)
                query = query.Where(p => p.LoanId == filter.LoanId.Value);

            if (filter.BorrowerId.HasValue)
                query = query.Where(p => p.Loan.BorrowerId == filter.BorrowerId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(p => p.PaymentDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(p => p.PaymentDate <= filter.ToDate.Value);

            if (filter.PaymentMethod.HasValue)
                query = query.Where(p => p.PaymentMethod == filter.PaymentMethod.Value);

            return await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }

        public async Task<string> GenerateReceipt(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Loan)
                    .ThenInclude(l => l.Borrower)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                throw new Exception("Payment not found");

            // Generate receipt content (can be enhanced with HTML template)
            return $@"
                PAYMENT RECEIPT
                Receipt #: {payment.ReceiptNumber}
                Date: {payment.PaymentDate:yyyy-MM-dd}
                
                Borrower: {payment.Loan.Borrower.FullName}
                Loan #: {payment.Loan.LoanNumber}
                
                Amount Received: ₦{payment.Amount:N2}
                Payment Method: {payment.PaymentMethod}
                
                Allocation:
                - Penalties: ₦{payment.PenaltyPortion:N2}
                - Interest: ₦{payment.InterestPortion:N2}
                - Principal: ₦{payment.PrincipalPortion:N2}
                
                Outstanding Balance: ₦{payment.Loan.OutstandingPrincipal:N2}
            ";
        }

        private async Task<string> GeneratePaymentNumber()
        {
            var year = DateTime.UtcNow.Year;
            var lastPayment = await _context.Payments
                .Where(p => p.PaymentNumber.StartsWith($"PAY-{year}-"))
                .OrderByDescending(p => p.PaymentNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastPayment != null)
            {
                var lastNumber = int.Parse(lastPayment.PaymentNumber.Split('-')[2]);
                nextNumber = lastNumber + 1;
            }

            return $"PAY-{year}-{nextNumber:D3}";
        }
    }
}
