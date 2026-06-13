using LoanApplication.Data;
using LoanApplication.Domain.Enums;
using LoanApplication.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Services
{
    // ─── Interface ────────────────────────────────────────────────────────────

    public interface IReportService
    {
        Task<DisbursementReportResponse> GetDisbursementReport(DisbursementReportRequest request);
        Task<OverdueAgingResponse> GetOverdueAgingReport();
        Task<CollectorPerformanceResponse> GetCollectorPerformance(CollectorPerformanceRequest request);
        Task<BorrowerActivityResponse> GetBorrowerActivity(int borrowerId, BorrowerActivityRequest request);
    }

    // ─── Implementation ───────────────────────────────────────────────────────

    public class ReportService : IReportService
    {
        private readonly LoanManagementDbContext _context;

        public ReportService(LoanManagementDbContext context)
        {
            _context = context;
        }

        // ── 1. Disbursement Report ────────────────────────────────────────────

        public async Task<DisbursementReportResponse> GetDisbursementReport(DisbursementReportRequest request)
        {
            var from = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
            var to   = (request.ToDate ?? DateTime.UtcNow).Date.AddDays(1); // inclusive

            var query = _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.CreatedBy)
                .Include(l => l.ApprovedBy)
                .Where(l => l.DisbursementDate != null
                         && l.DisbursementDate >= from
                         && l.DisbursementDate <  to);

            if (request.OfficerId.HasValue)
                query = query.Where(l => l.ApprovedByUserId == request.OfficerId ||
                                         l.CreatedByUserId  == request.OfficerId);

            if (request.MinAmount.HasValue)
                query = query.Where(l => l.ApprovedAmount >= request.MinAmount);

            if (request.MaxAmount.HasValue)
                query = query.Where(l => l.ApprovedAmount <= request.MaxAmount);

            var totalCount = await query.CountAsync();
            var loans = await query
                .OrderByDescending(l => l.DisbursementDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // All (unpaged) for totals + officer breakdown
            var allLoans = await query.ToListAsync();

            var items = loans.Select(l => new DisbursementItem
            {
                LoanId             = l.LoanId,
                LoanNumber         = l.LoanNumber,
                BorrowerName       = l.Borrower?.FullName ?? "",
                BorrowerPhone      = l.Borrower?.PhoneNumber ?? "",
                AmountDisbursed    = l.ApprovedAmount,
                RequestedAmount    = l.RequestedAmount,
                DisbursementMethod = l.DisbursementMethod.HasValue
                    ? l.DisbursementMethod.Value.ToString() : "N/A",
                DisbursedOn        = l.DisbursementDate!.Value.UtcDateTime,
                ApprovedByOfficer  = l.ApprovedBy?.FullName ?? "—",
                CreatedByOfficer   = l.CreatedBy?.FullName  ?? "—",
                DurationMonths     = l.DurationInMonths,
                Phase1Rate         = l.Phase1InterestRate,
                Phase2Rate         = l.Phase2InterestRate,
                Status             = l.Status.ToString()
            }).ToList();

            var byOfficer = allLoans
                .GroupBy(l => l.ApprovedBy?.FullName ?? l.CreatedBy?.FullName ?? "Unknown")
                .Select(g => new OfficerDisbursementSummary
                {
                    OfficerName        = g.Key,
                    LoanCount          = g.Count(),
                    TotalDisbursed     = g.Sum(l => l.ApprovedAmount),
                    AverageLoanAmount  = g.Average(l => l.ApprovedAmount)
                })
                .OrderByDescending(o => o.TotalDisbursed)
                .ToList();

            return new DisbursementReportResponse
            {
                TotalDisbursements   = totalCount,
                TotalAmountDisbursed = allLoans.Sum(l => l.ApprovedAmount),
                AverageLoanSize      = allLoans.Any() ? allLoans.Average(l => l.ApprovedAmount) : 0,
                Items                = items,
                ByOfficer            = byOfficer,
                TotalPages           = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                CurrentPage          = request.PageNumber
            };
        }

        // ── 2. Overdue Aging Report ───────────────────────────────────────────

        public async Task<OverdueAgingResponse> GetOverdueAgingReport()
        {
            var today = DateTime.UtcNow.Date;

            // All loans with overdue unpaid installments
            var overdueLoans = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.RepaymentSchedules)
                .Include(l => l.Payments)
                .Where(l => l.Status == LoanStatus.Overdue
                         || l.Status == LoanStatus.AtRisk
                         || l.Status == LoanStatus.Defaulted
                         || l.Status == LoanStatus.Partial)
                .ToListAsync();

            var totalPortfolio = await _context.Loans
                .Where(l => l.Status != LoanStatus.Closed)
                .SumAsync(l => l.OutstandingPrincipal);

            var agingItems = overdueLoans
                .Select(l =>
                {
                    // Find the earliest unpaid overdue schedule entry
                    var earliestOverdue = l.RepaymentSchedules
                        .Where(rs => !rs.IsPaid && rs.DueDate.Date < today)
                        .OrderBy(rs => rs.DueDate)
                        .FirstOrDefault();

                    if (earliestOverdue == null) return null;

                    var daysOverdue = (today - earliestOverdue.DueDate.Date).Days;
                    var overdueSchedules = l.RepaymentSchedules
                        .Where(rs => !rs.IsPaid && rs.DueDate.Date < today)
                        .ToList();
                    var overdueAmount = overdueSchedules.Sum(rs => rs.TotalAmount - rs.AmountPaid);
                    var lastPayment   = l.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

                    return new OverdueAgingItem
                    {
                        LoanId              = l.LoanId,
                        LoanNumber          = l.LoanNumber,
                        BorrowerName        = l.Borrower?.FullName    ?? "",
                        BorrowerPhone       = l.Borrower?.PhoneNumber ?? "",
                        GuarantorName       = l.Borrower?.GuarantorName  ?? "",
                        GuarantorPhone      = l.Borrower?.GuarantorPhone ?? "",
                        OutstandingBalance  = l.OutstandingPrincipal,
                        TotalOverdueAmount  = overdueAmount,
                        DaysOverdue         = daysOverdue,
                        LastPaymentDate     = lastPayment?.PaymentDate.UtcDateTime ?? l.DisbursementDate?.UtcDateTime ?? DateTime.MinValue,
                        TotalPenalties      = l.TotalPenaltiesAccrued,
                        Status              = l.Status.ToString(),
                        AgingBucket         = daysOverdue <= 30  ? "1-30 days"  :
                                              daysOverdue <= 60  ? "31-60 days" :
                                              daysOverdue <= 90  ? "61-90 days" : "90+ days"
                    };
                })
                .Where(x => x != null)
                .Cast<OverdueAgingItem>()
                .OrderByDescending(x => x.DaysOverdue)
                .ToList();

            OverdueAgingBucket MakeBucket(string label, IEnumerable<OverdueAgingItem> src)
            {
                var list = src.ToList();
                var total = list.Sum(x => x.TotalOverdueAmount);
                return new OverdueAgingBucket
                {
                    Label      = label,
                    LoanCount  = list.Count,
                    TotalAmount= total,
                    PARPercent = totalPortfolio > 0 ? (total / totalPortfolio * 100) : 0,
                    Items      = list
                };
            }

            var totalOverdue = agingItems.Sum(x => x.TotalOverdueAmount);

            return new OverdueAgingResponse
            {
                Bucket1_1_30   = MakeBucket("1–30 Days",  agingItems.Where(x => x.DaysOverdue <= 30)),
                Bucket2_31_60  = MakeBucket("31–60 Days", agingItems.Where(x => x.DaysOverdue > 30 && x.DaysOverdue <= 60)),
                Bucket3_61_90  = MakeBucket("61–90 Days", agingItems.Where(x => x.DaysOverdue > 60 && x.DaysOverdue <= 90)),
                Bucket4_90Plus = MakeBucket("90+ Days",   agingItems.Where(x => x.DaysOverdue > 90)),
                TotalOverdueAmount        = totalOverdue,
                TotalOverdueCount         = agingItems.Count,
                TotalPortfolioOutstanding = totalPortfolio,
                OverallPARPercentage      = totalPortfolio > 0 ? (totalOverdue / totalPortfolio * 100) : 0,
                Items                     = agingItems
            };
        }

        // ── 3. Collector Performance Report ──────────────────────────────────

        public async Task<CollectorPerformanceResponse> GetCollectorPerformance(CollectorPerformanceRequest request)
        {
            var from = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
            var to   = (request.ToDate ?? DateTime.UtcNow).Date.AddDays(1);

            var paymentsQuery = _context.Payments
                .Include(p => p.RecordedBy)
                .Include(p => p.Loan)
                .Where(p => p.PaymentDate >= from && p.PaymentDate < to);

            if (request.OfficerId.HasValue)
                paymentsQuery = paymentsQuery.Where(p => p.RecordedByUserId == request.OfficerId);

            var payments = await paymentsQuery.ToListAsync();

            var collectorGroups = payments
                .GroupBy(p => new { p.RecordedByUserId, Name = p.RecordedBy?.FullName ?? "Unknown", Role = p.RecordedBy?.Role.ToString() ?? "" })
                .ToList();

            var collectors = collectorGroups.Select(g =>
            {
                var payList = g.ToList();
                var daily = payList
                    .GroupBy(p => p.PaymentDate.Date)
                    .Select(d => new CollectorDailyBreakdown
                    {
                        Date             = d.Key,
                        PaymentCount     = d.Count(),
                        AmountCollected  = d.Sum(p => p.Amount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                return new CollectorSummary
                {
                    OfficerId             = g.Key.RecordedByUserId,
                    OfficerName           = g.Key.Name,
                    Role                  = g.Key.Role,
                    PaymentsRecorded      = payList.Count,
                    TotalAmountCollected  = payList.Sum(p => p.Amount),
                    PrincipalCollected    = payList.Sum(p => p.PrincipalPortion),
                    InterestCollected     = payList.Sum(p => p.InterestPortion),
                    PenaltiesCollected    = payList.Sum(p => p.PenaltyPortion),
                    UniqueBorrowersServed = payList.Select(p => p.Loan?.BorrowerId).Distinct().Count(),
                    UniqueLoansHandled    = payList.Select(p => p.LoanId).Distinct().Count(),
                    AveragePaymentSize    = payList.Any() ? payList.Average(p => p.Amount) : 0,
                    DailyBreakdown        = daily
                };
            })
            .OrderByDescending(c => c.TotalAmountCollected)
            .ToList();

            return new CollectorPerformanceResponse
            {
                FromDate               = from,
                ToDate                 = to.AddDays(-1),
                TotalCollected         = payments.Sum(p => p.Amount),
                TotalPaymentsRecorded  = payments.Count,
                Collectors             = collectors
            };
        }

        // ── 4. Borrower Activity Report ───────────────────────────────────────

        public async Task<BorrowerActivityResponse> GetBorrowerActivity(int borrowerId, BorrowerActivityRequest request)
        {
            var from = request.FromDate;
            var to   = request.ToDate.HasValue ? request.ToDate.Value.Date.AddDays(1) : (DateTime?)null;

            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.BorrowerId == borrowerId)
                ?? throw new KeyNotFoundException($"Borrower {borrowerId} not found.");

            // Loans
            var loansQuery = _context.Loans
                .Include(l => l.Payments)
                .Include(l => l.ApprovedBy)
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.BorrowerId == borrowerId);

            if (from.HasValue)
                loansQuery = loansQuery.Where(l => l.ApplicationDate >= from);
            if (to.HasValue)
                loansQuery = loansQuery.Where(l => l.ApplicationDate < to);

            var loans = await loansQuery.OrderByDescending(l => l.ApplicationDate).ToListAsync();

            // Payments (across all loans of this borrower, optionally date-filtered)
            var paymentsQuery = _context.Payments
                .Include(p => p.RecordedBy)
                .Include(p => p.Loan)
                .Where(p => p.Loan.BorrowerId == borrowerId);

            if (from.HasValue)
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= from);
            if (to.HasValue)
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate < to);

            var payments = await paymentsQuery.OrderByDescending(p => p.PaymentDate).ToListAsync();

            // Audit trail (borrower entity + their loans)
            var loanIds = loans.Select(l => l.LoanId).ToList();
            var auditQuery = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => (a.EntityType == "Borrower" && a.EntityId == borrowerId)
                         || (a.EntityType == "Loan"     && loanIds.Contains(a.EntityId ?? 0))
                         || (a.EntityType == "Payment"  && payments.Select(p => p.PaymentId).Contains(a.EntityId ?? 0)));

            if (from.HasValue)
                auditQuery = auditQuery.Where(a => a.CreatedAt >= from);
            if (to.HasValue)
                auditQuery = auditQuery.Where(a => a.CreatedAt < to);

            var auditLogs = await auditQuery.OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync();

            // Missed payments = unpaid schedule entries past due date
            var today = DateTime.UtcNow.Date;
            var allSchedules = loans.SelectMany(l => l.RepaymentSchedules).ToList();
            var missedCount  = allSchedules.Count(rs => !rs.IsPaid && rs.DueDate.Date < today);

            // Build response
            var profile = new BorrowerActivityProfile
            {
                BorrowerId         = borrower.BorrowerId,
                FullName           = borrower.FullName,
                PhoneNumber        = borrower.PhoneNumber,
                Email              = borrower.Email ?? "",
                NationalId         = borrower.NationalIdNumber,
                HomeAddress        = borrower.HomeAddress ?? "",
                EmployerOrBusiness = borrower.EmployerOrBusiness ?? "",
                GuarantorName      = borrower.GuarantorName  ?? "",
                GuarantorPhone     = borrower.GuarantorPhone ?? "",
                CreditScore        = borrower.InternalCreditScore.ToString(),
                OnTimePaymentPct   = borrower.OnTimePaymentPercentage,
                IsActive           = borrower.IsActive,
                MemberSince        = borrower.CreatedAt.UtcDateTime
            };

            var summary = new BorrowerActivitySummary
            {
                TotalLoans         = loans.Count,
                ActiveLoans        = loans.Count(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue
                                                   || l.Status == LoanStatus.Due    || l.Status == LoanStatus.AtRisk
                                                   || l.Status == LoanStatus.Partial),
                ClosedLoans        = loans.Count(l => l.Status == LoanStatus.Closed),
                DefaultedLoans     = loans.Count(l => l.Status == LoanStatus.Defaulted),
                TotalBorrowed      = loans.Sum(l => l.ApprovedAmount),
                TotalRepaid        = payments.Sum(p => p.Amount),
                TotalOutstanding   = loans.Sum(l => l.OutstandingPrincipal),
                TotalInterestPaid  = payments.Sum(p => p.InterestPortion),
                TotalPenaltiesPaid = payments.Sum(p => p.PenaltyPortion),
                TotalPaymentsMade  = payments.Count,
                MissedPayments     = missedCount
            };

            var loanActivity = loans.Select(l => new BorrowerLoanActivity
            {
                LoanId            = l.LoanId,
                LoanNumber        = l.LoanNumber,
                ApprovedAmount    = l.ApprovedAmount,
                OutstandingBalance= l.OutstandingPrincipal,
                Status            = l.Status.ToString(),
                DisbursedOn       = l.DisbursementDate?.UtcDateTime,
                ClosedOn          = l.ClosedDate?.UtcDateTime,
                DurationMonths    = l.DurationInMonths,
                Phase1Rate        = l.Phase1InterestRate,
                TotalPaid         = l.Payments.Sum(p => p.Amount),
                TotalInterest     = l.Payments.Sum(p => p.InterestPortion),
                TotalPenalties    = l.Payments.Sum(p => p.PenaltyPortion),
                ApprovedBy        = l.ApprovedBy?.FullName ?? "—"
            }).ToList();

            var paymentActivity = payments.Select(p => new BorrowerPaymentActivity
            {
                PaymentId        = p.PaymentId,
                PaymentNumber    = p.PaymentNumber,
                LoanNumber       = p.Loan?.LoanNumber ?? "",
                Amount           = p.Amount,
                PrincipalPortion = p.PrincipalPortion,
                InterestPortion  = p.InterestPortion,
                PenaltyPortion   = p.PenaltyPortion,
                PaymentMethod    = p.PaymentMethod.ToString(),
                PaymentDate      = p.PaymentDate.UtcDateTime,
                RecordedBy       = p.RecordedBy?.FullName ?? "",
                Notes            = p.Notes ?? ""
            }).ToList();

            var auditTrail = auditLogs.Select(a => new BorrowerAuditEntry
            {
                Action      = a.Action,
                EntityType  = a.EntityType,
                EntityId    = a.EntityId,
                PerformedBy = a.User?.FullName ?? $"User #{a.UserId}",
                Timestamp   = a.CreatedAt.UtcDateTime,
                Details     = a.NewValues ?? a.Reason ?? ""
            }).ToList();

            return new BorrowerActivityResponse
            {
                Profile    = profile,
                Summary    = summary,
                Loans      = loanActivity,
                Payments   = paymentActivity,
                AuditTrail = auditTrail
            };
        }
    }
}
