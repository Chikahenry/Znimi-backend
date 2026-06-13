using LoanApplication.Data;
using LoanApplication.Domain.Enums;
using LoanApplication.Domain.Models;
using LoanApplication.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Services
{
    public interface IBorrowerService
    {
        Task<Borrower> CreateBorrower(CreateBorrowerRequest request);
        Task<Borrower> UpdateBorrower(int borrowerId, CreateBorrowerRequest request);
        Task<Borrower> GetBorrowerDetails(int borrowerId);
        Task<List<Borrower>> GetBorrowersWithFilters(BorrowerFilterRequest filter);
        Task<BorrowerStatementResponse> GenerateBorrowerStatement(int borrowerId, int loanId);
        Task UpdateBorrowerCreditScore(int borrowerId);
        Task<List<Loan>> GetBorrowerLoanHistory(int borrowerId);
    }


    public class BorrowerService : IBorrowerService
    {
        private readonly LoanManagementDbContext _context;

        public BorrowerService(LoanManagementDbContext context)
        {
            _context = context;
        }

        public async Task<Borrower> CreateBorrower(CreateBorrowerRequest request)
        {
            var existingBorrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.NationalIdNumber == request.NationalIdNumber);

            if (existingBorrower != null)
                throw new Exception("Borrower with this National ID already exists");

            var borrower = new Borrower
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                AlternatePhoneNumber = request.AlternatePhoneNumber,
                Email = request.Email,
                NationalIdNumber = request.NationalIdNumber,
                HomeAddress = request.HomeAddress,
                EmployerOrBusiness = request.EmployerOrBusiness,
                GuarantorName = request.GuarantorName,
                GuarantorPhone = request.GuarantorPhone
            };

            _context.Borrowers.Add(borrower);
            await _context.SaveChangesAsync();

            return borrower;
        }

        public async Task<Borrower> UpdateBorrower(int borrowerId, CreateBorrowerRequest request)
        {
            var borrower = await _context.Borrowers.FindAsync(borrowerId);
            if (borrower == null)
                throw new Exception("Borrower not found");

            borrower.FullName = request.FullName;
            borrower.PhoneNumber = request.PhoneNumber;
            borrower.AlternatePhoneNumber = request.AlternatePhoneNumber;
            borrower.Email = request.Email;
            borrower.HomeAddress = request.HomeAddress;
            borrower.EmployerOrBusiness = request.EmployerOrBusiness;
            borrower.GuarantorName = request.GuarantorName;
            borrower.GuarantorPhone = request.GuarantorPhone;
            borrower.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return borrower;
        }

        public async Task<Borrower> GetBorrowerDetails(int borrowerId)
        {
            return await _context.Borrowers
                .Include(b => b.Loans)
                .Include(b => b.Documents)
                .FirstOrDefaultAsync(b => b.BorrowerId == borrowerId);
        }

        public async Task<List<Borrower>> GetBorrowersWithFilters(BorrowerFilterRequest filter)
        {
            var query = _context.Borrowers.AsQueryable();

            if (filter.CreditScore.HasValue)
                query = query.Where(b => b.InternalCreditScore == filter.CreditScore.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(b => b.IsActive == filter.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(b =>
                    b.FullName.ToLower().Contains(searchTerm) ||
                    b.PhoneNumber.Contains(searchTerm) ||
                    b.NationalIdNumber.Contains(searchTerm));
            }

            return await query
                .OrderBy(b => b.FullName)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }

        public async Task<BorrowerStatementResponse> GenerateBorrowerStatement(int borrowerId, int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Payments)
                    .ThenInclude(p => p.RecordedBy)
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.LoanId == loanId && l.BorrowerId == borrowerId);

            if (loan == null)
                throw new Exception("Loan not found for this borrower");

            var nextSchedule = loan.RepaymentSchedules
                .Where(rs => !rs.IsPaid)
                .OrderBy(rs => rs.DueDate)
                .FirstOrDefault();

            return new BorrowerStatementResponse
            {
                BorrowerName = loan.Borrower.FullName,
                LoanNumber = loan.LoanNumber,
                LoanAmount = loan.ApprovedAmount,
                InterestRate = loan.Phase1InterestRate,
                DurationInMonths = loan.DurationInMonths,
                PaymentHistory = loan.Payments.Select(p => new PaymentHistoryDto
                {
                    PaymentNumber = p.PaymentNumber,
                    PaymentDate = p.PaymentDate.ToUniversalTime(),
                    Amount = p.Amount,
                    PenaltyPortion = p.PenaltyPortion,
                    InterestPortion = p.InterestPortion,
                    PrincipalPortion = p.PrincipalPortion,
                    PaymentMethod = p.PaymentMethod.ToString(),
                    RecordedBy = p.RecordedBy.FullName
                }).ToList(),
                OutstandingBalance = loan.OutstandingPrincipal,
                NextPaymentDueDate = nextSchedule?.DueDate.ToUniversalTime(),
                NextPaymentAmount = nextSchedule?.TotalAmount ?? 0
            };
        }

        public async Task UpdateBorrowerCreditScore(int borrowerId)
        {
            var borrower = await _context.Borrowers
                .Include(b => b.Loans)
                    .ThenInclude(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(b => b.BorrowerId == borrowerId);

            if (borrower == null) return;

            var allSchedules = borrower.Loans
                .SelectMany(l => l.RepaymentSchedules)
                .Where(rs => rs.DueDate < DateTime.UtcNow)
                .ToList();

            if (!allSchedules.Any()) return;

            var paidOnTime = allSchedules.Count(rs => rs.IsPaid && rs.PaidDate <= rs.DueDate.AddDays(3));
            var totalDue = allSchedules.Count;

            borrower.OnTimePaymentPercentage = (decimal)paidOnTime / totalDue * 100;
            borrower.TotalLoansCount = borrower.Loans.Count;
            borrower.DefaultedLoansCount = borrower.Loans.Count(l => l.Status == LoanStatus.Defaulted);

            // Calculate credit score
            if (borrower.OnTimePaymentPercentage >= 95 && borrower.DefaultedLoansCount == 0)
                borrower.InternalCreditScore = CreditScore.A;
            else if (borrower.OnTimePaymentPercentage >= 80 && borrower.DefaultedLoansCount == 0)
                borrower.InternalCreditScore = CreditScore.B;
            else if (borrower.OnTimePaymentPercentage >= 60 || borrower.DefaultedLoansCount <= 1)
                borrower.InternalCreditScore = CreditScore.C;
            else
                borrower.InternalCreditScore = CreditScore.D;

            await _context.SaveChangesAsync();
        }

        public async Task<List<Loan>> GetBorrowerLoanHistory(int borrowerId)
        {
            return await _context.Loans
                .Where(l => l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.ApplicationDate)
                .ToListAsync();
        }
    }
}
