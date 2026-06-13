using LoanApplication.Data;
using LoanApplication.Domain.Enums;
using LoanApplication.Domain.Models;
using LoanApplication.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Services
{
    public interface ILoanService
    {
        Task<Loan> CreateLoanApplication(CreateLoanApplicationRequest request, int createdByUserId);
        Task<Loan> ApproveLoan(ApproveLoanRequest request, int approvedByUserId);
        Task<Loan> DisburseLoan(DisburseLoanRequest request);
        Task<LoanDetailsResponse> GetLoanDetails(int loanId);
        Task<List<Loan>> GetLoansWithFilters(LoanFilterRequest filter);
        Task UpdateLoanStatuses();
        Task<List<RepaymentSchedule>> GenerateRepaymentSchedule(int loanId);
    }


    public class LoanService : ILoanService
    {
        private readonly LoanManagementDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IWhatsAppService _whatsAppService;
        
        public LoanService(LoanManagementDbContext context,
            IWhatsAppService whatsAppService,IAuditService auditService)
        {
            _whatsAppService = whatsAppService;
            _context = context;
            _auditService = auditService;
        }

        public async Task<Loan> CreateLoanApplication(CreateLoanApplicationRequest request, int createdByUserId)
        {
            var borrower = await _context.Borrowers.FindAsync(request.BorrowerId);
            if (borrower == null)
                throw new Exception("Borrower not found");

            var loanNumber = await GenerateLoanNumber();

            var loan = new Loan
            {
                LoanNumber = loanNumber,
                BorrowerId = request.BorrowerId,
                CreatedByUserId = createdByUserId,
                RequestedAmount = request.RequestedAmount,
                ApprovedAmount = request.RequestedAmount,
                OutstandingPrincipal = request.RequestedAmount,
                DurationInMonths = request.DurationInMonths,
                Status = LoanStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                Notes = request.Notes
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            await _auditService.LogAction(createdByUserId, "CreateLoanApplication", "Loan", loan.LoanId, "null", loan);

            return loan;
        }

        public async Task<Loan> ApproveLoan(ApproveLoanRequest request, int approvedByUserId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.LoanId == request.LoanId);

            if (loan == null)
                throw new Exception("Loan not found");

            if (loan.Status != LoanStatus.Pending)
                throw new Exception("Only pending loans can be approved");

            var oldValues = new { loan.ApprovedAmount, loan.DurationInMonths, loan.Phase1InterestRate, loan.Phase2InterestRate };

            if (request.ApprovedAmount.HasValue)
                loan.ApprovedAmount = request.ApprovedAmount.Value;

            if (request.ModifiedDuration.HasValue)
                loan.DurationInMonths = request.ModifiedDuration.Value;

            if (request.CustomPhase1Rate.HasValue)
                loan.Phase1InterestRate = request.CustomPhase1Rate.Value;

            if (request.CustomPhase2Rate.HasValue)
                loan.Phase2InterestRate = request.CustomPhase2Rate.Value;

            loan.OutstandingPrincipal = loan.ApprovedAmount;
            loan.Status = LoanStatus.Approved;
            loan.ApprovalDate = DateTime.UtcNow;
            loan.ApprovedByUserId = approvedByUserId;

            await _context.SaveChangesAsync();
            // Send WhatsApp notification
            await _whatsAppService.SendLoanApprovalNotification(loan.LoanId);
            await _auditService.LogAction(approvedByUserId, "ApproveLoan", "Loan", loan.LoanId, oldValues, loan);

            return loan;
        }

        public async Task<Loan> DisburseLoan(DisburseLoanRequest request)
        {
            var loan = await _context.Loans.FindAsync(request.LoanId);

            if (loan == null)
                throw new Exception("Loan not found");

            if (loan.Status != LoanStatus.Approved)
                throw new Exception("Only approved loans can be disbursed");

            loan.DisbursementDate = request.DisbursementDate.ToUniversalTime();
            loan.DisbursementMethod = request.DisbursementMethod;
            loan.Status = LoanStatus.Active;

            // Calculate first payment due date
            var disbursementDate = request.DisbursementDate;
            var firstPaymentDate = new DateTime(
                disbursementDate.Year,
                disbursementDate.Month,
                loan.RepaymentDay
            );

            if (firstPaymentDate <= disbursementDate)
                firstPaymentDate = firstPaymentDate.AddMonths(1);

            loan.FirstPaymentDueDate = firstPaymentDate.ToUniversalTime();

           //await _context.SaveChangesAsync();
            // Send WhatsApp notification
            await _whatsAppService.SendLoanDisbursementNotification(loan.LoanId);
            // Generate repayment schedule
            await GenerateRepaymentSchedule(loan.LoanId);

            return loan;
        }

        public async Task<List<RepaymentSchedule>> GenerateRepaymentSchedule(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            if (loan == null || !loan.FirstPaymentDueDate.HasValue)
                throw new Exception("Loan not found or not disbursed");

            // Clear existing schedule
            var existingSchedules = _context.RepaymentSchedules.Where(rs => rs.LoanId == loanId);
            _context.RepaymentSchedules.RemoveRange(existingSchedules);

            var schedules = new List<RepaymentSchedule>();
            var remainingPrincipal = loan.ApprovedAmount;
            var principalPerMonth = loan.ApprovedAmount / loan.DurationInMonths;
            var currentDueDate = loan.FirstPaymentDueDate.Value;

            for (int i = 1; i <= loan.DurationInMonths; i++)
            {
                var phase = i <= 3 ? InterestPhase.ReducingBalance : InterestPhase.FlatRate;
                decimal interestAmount;

                if (phase == InterestPhase.ReducingBalance)
                {
                    interestAmount = remainingPrincipal * (loan.Phase1InterestRate / 100m);
                }
                else
                {
                    interestAmount = loan.ApprovedAmount * (loan.Phase2InterestRate / 100m);
                }

                var principalAmount = principalPerMonth;
                if (i == loan.DurationInMonths) // Last payment
                    principalAmount = remainingPrincipal;

                remainingPrincipal -= principalAmount;

                var schedule = new RepaymentSchedule
                {
                    LoanId = loanId,
                    InstallmentNumber = i,
                    DueDate = currentDueDate,
                    PrincipalAmount = principalAmount,
                    InterestAmount = interestAmount,
                    TotalAmount = principalAmount + interestAmount,
                    BalanceAfterPayment = remainingPrincipal,
                    InterestPhase = phase
                };

                schedules.Add(schedule);
                currentDueDate = currentDueDate.AddMonths(1);
            }

            _context.RepaymentSchedules.AddRange(schedules);
            await _context.SaveChangesAsync();

            return schedules;
        }

        public async Task<LoanDetailsResponse> GetLoanDetails(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.RepaymentSchedules)
                .Include(l => l.Payments)
                    .ThenInclude(p => p.RecordedBy)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null)
                throw new Exception("Loan not found");

            return new LoanDetailsResponse
            {
                LoanId = loan.LoanId,
                LoanNumber = loan.LoanNumber,
                BorrowerName = loan.Borrower.FullName,
                ApprovedAmount = loan.ApprovedAmount,
                OutstandingPrincipal = loan.OutstandingPrincipal,
                TotalInterestAccrued = loan.TotalInterestAccrued,
                TotalPenaltiesAccrued = loan.TotalPenaltiesAccrued,
                Status = loan.Status,
                DisbursementDate = loan.DisbursementDate,
                FirstPaymentDueDate = loan.FirstPaymentDueDate,
                RepaymentSchedule = loan.RepaymentSchedules.Select(rs => new RepaymentScheduleDto
                {
                    InstallmentNumber = rs.InstallmentNumber,
                    DueDate = rs.DueDate,
                    PrincipalAmount = rs.PrincipalAmount,
                    InterestAmount = rs.InterestAmount,
                    TotalAmount = rs.TotalAmount,
                    BalanceAfterPayment = rs.BalanceAfterPayment,
                    InterestPhase = rs.InterestPhase.ToString(),
                    IsPaid = rs.IsPaid,
                    AmountPaid = rs.AmountPaid
                }).ToList(),
                PaymentHistory = loan.Payments.Select(p => new PaymentHistoryDto
                {
                    PaymentNumber = p.PaymentNumber,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    PenaltyPortion = p.PenaltyPortion,
                    InterestPortion = p.InterestPortion,
                    PrincipalPortion = p.PrincipalPortion,
                    PaymentMethod = p.PaymentMethod.ToString(),
                    RecordedBy = p.RecordedBy.FullName
                }).ToList()
            };
        }

        public async Task<List<Loan>> GetLoansWithFilters(LoanFilterRequest filter)
        {
            try
            {
                var query = _context.Loans
                    .Include(l => l.Borrower)
                    .Include(l => l.CreatedBy)
                    .AsQueryable();

                if (filter.Status.HasValue)
                    query = query.Where(l => l.Status == filter.Status.Value);

                if (filter.BorrowerId.HasValue)
                    query = query.Where(l => l.BorrowerId == filter.BorrowerId.Value);

                if (filter.FromDate.HasValue)
                    query = query.Where(l => l.ApplicationDate >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(l => l.ApplicationDate <= filter.ToDate.Value);

                if (filter.MinAmount.HasValue)
                    query = query.Where(l => l.ApprovedAmount >= filter.MinAmount.Value);

                if (filter.MaxAmount.HasValue)
                    query = query.Where(l => l.ApprovedAmount <= filter.MaxAmount.Value);

                if (filter.CreatedByUserId.HasValue)
                    query = query.Where(l => l.CreatedByUserId == filter.CreatedByUserId.Value);

                // Sorting
                query = filter.SortBy?.ToLower() switch
                {
                    "amount" => filter.SortDescending
                        ? query.OrderByDescending(l => l.ApprovedAmount)
                        : query.OrderBy(l => l.ApprovedAmount),
                    "status" => filter.SortDescending
                        ? query.OrderByDescending(l => l.Status)
                        : query.OrderBy(l => l.Status),
                    _ => filter.SortDescending
                        ? query.OrderByDescending(l => l.ApplicationDate)
                        : query.OrderBy(l => l.ApplicationDate)
                };

                var result  = await query
                            .Skip((filter.PageNumber - 1) * filter.PageSize)
                            .Take(filter.PageSize)
                            .ToListAsync();

                return result;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task UpdateLoanStatuses()
        {
            var activeLoans = await _context.Loans
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.Status == LoanStatus.Active ||
                           l.Status == LoanStatus.Upcoming ||
                           l.Status == LoanStatus.Overdue ||
                           l.Status == LoanStatus.Partial)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;

            foreach (var loan in activeLoans)
            {
                var nextUnpaidSchedule = loan.RepaymentSchedules
                    .Where(rs => !rs.IsPaid)
                    .OrderBy(rs => rs.DueDate)
                    .FirstOrDefault();

                if (nextUnpaidSchedule == null)
                {
                    loan.Status = LoanStatus.Closed;
                    loan.ClosedDate = DateTime.UtcNow;
                    continue;
                }

                var daysDifference = (nextUnpaidSchedule.DueDate.Date - today).Days;
                var daysOverdue = (today - nextUnpaidSchedule.DueDate.Date).Days;

                if (daysOverdue > 90)
                {
                    loan.Status = LoanStatus.Defaulted;
                }
                else if (daysOverdue > 30)
                {
                    loan.Status = LoanStatus.AtRisk;
                }
                else if (daysOverdue > loan.GracePeriodDays)
                {
                    loan.Status = LoanStatus.Overdue;

                    // Calculate penalties
                    var penaltyDays = daysOverdue - loan.GracePeriodDays;
                    var dailyPenalty = loan.DailyPenaltyAmount > 0
                        ? loan.DailyPenaltyAmount
                        : (nextUnpaidSchedule.TotalAmount * loan.PenaltyPercentage / 100m);

                    loan.TotalPenaltiesAccrued += dailyPenalty * penaltyDays;
                }
                else if (daysDifference >= 1 && daysDifference <= 5)
                {
                    loan.Status = LoanStatus.Upcoming;
                }
                else if (daysDifference == 0)
                {
                    loan.Status = LoanStatus.Due;
                }
                else
                {
                    loan.Status = LoanStatus.Active;
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateLoanNumber()
        {
            var year = DateTime.UtcNow.Year;
            var lastLoan = await _context.Loans
                .Where(l => l.LoanNumber.StartsWith($"LN-{year}-"))
                .OrderByDescending(l => l.LoanNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastLoan != null)
            {
                var lastNumber = int.Parse(lastLoan.LoanNumber.Split('-')[2]);
                nextNumber = lastNumber + 1;
            }

            return $"LN-{year}-{nextNumber:D3}";
        }
    }
}
