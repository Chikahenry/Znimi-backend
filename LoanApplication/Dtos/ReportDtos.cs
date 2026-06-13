using LoanApplication.Domain.Enums;

namespace LoanApplication.Dtos
{
    // ─── Shared ───────────────────────────────────────────────────────────────

    public class ReportFilterBase
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate   { get; set; }
        public int PageNumber     { get; set; } = 1;
        public int PageSize       { get; set; } = 50;
    }

    // ─── 1. Disbursement Report ───────────────────────────────────────────────

    public class DisbursementReportRequest : ReportFilterBase
    {
        public int? OfficerId { get; set; }       // Filter by loan officer who approved
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
    }

    public class DisbursementReportResponse
    {
        public int TotalDisbursements       { get; set; }
        public decimal TotalAmountDisbursed { get; set; }
        public decimal AverageLoanSize      { get; set; }
        public List<DisbursementItem> Items { get; set; } = new();
        public List<OfficerDisbursementSummary> ByOfficer { get; set; } = new();
        public int TotalPages               { get; set; }
        public int CurrentPage              { get; set; }
    }

    public class DisbursementItem
    {
        public int    LoanId           { get; set; }
        public string LoanNumber       { get; set; }
        public string BorrowerName     { get; set; }
        public string BorrowerPhone    { get; set; }
        public decimal AmountDisbursed { get; set; }
        public decimal RequestedAmount { get; set; }
        public string DisbursementMethod { get; set; }
        public DateTime DisbursedOn    { get; set; }
        public string ApprovedByOfficer { get; set; }
        public string CreatedByOfficer  { get; set; }
        public int    DurationMonths   { get; set; }
        public decimal Phase1Rate      { get; set; }
        public decimal Phase2Rate      { get; set; }
        public string Status           { get; set; }
    }

    public class OfficerDisbursementSummary
    {
        public string OfficerName         { get; set; }
        public int    LoanCount           { get; set; }
        public decimal TotalDisbursed     { get; set; }
        public decimal AverageLoanAmount  { get; set; }
    }

    // ─── 2. Overdue Aging Report ──────────────────────────────────────────────

    public class OverdueAgingResponse
    {
        public OverdueAgingBucket Bucket1_1_30   { get; set; }   // 1–30 days
        public OverdueAgingBucket Bucket2_31_60  { get; set; }   // 31–60 days
        public OverdueAgingBucket Bucket3_61_90  { get; set; }   // 61–90 days
        public OverdueAgingBucket Bucket4_90Plus { get; set; }   // 90+ days
        public decimal TotalOverdueAmount        { get; set; }
        public int     TotalOverdueCount         { get; set; }
        public decimal TotalPortfolioOutstanding { get; set; }
        public decimal OverallPARPercentage      { get; set; }
        public List<OverdueAgingItem> Items      { get; set; } = new();
    }

    public class OverdueAgingBucket
    {
        public string Label         { get; set; }
        public int    LoanCount     { get; set; }
        public decimal TotalAmount  { get; set; }
        public decimal PARPercent   { get; set; }
        public List<OverdueAgingItem> Items { get; set; } = new();
    }

    public class OverdueAgingItem
    {
        public int    LoanId              { get; set; }
        public string LoanNumber          { get; set; }
        public string BorrowerName        { get; set; }
        public string BorrowerPhone       { get; set; }
        public string GuarantorName       { get; set; }
        public string GuarantorPhone      { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal TotalOverdueAmount { get; set; }
        public int    DaysOverdue         { get; set; }
        public DateTime LastPaymentDate   { get; set; }
        public decimal TotalPenalties     { get; set; }
        public string Status              { get; set; }
        public string AgingBucket         { get; set; }
    }

    // ─── 3. Collector Performance Report ──────────────────────────────────────

    public class CollectorPerformanceRequest : ReportFilterBase
    {
        public int? OfficerId { get; set; }
    }

    public class CollectorPerformanceResponse
    {
        public DateTime FromDate                      { get; set; }
        public DateTime ToDate                        { get; set; }
        public decimal TotalCollected                 { get; set; }
        public int     TotalPaymentsRecorded          { get; set; }
        public List<CollectorSummary> Collectors      { get; set; } = new();
    }

    public class CollectorSummary
    {
        public int    OfficerId               { get; set; }
        public string OfficerName             { get; set; }
        public string Role                    { get; set; }
        public int    PaymentsRecorded        { get; set; }
        public decimal TotalAmountCollected   { get; set; }
        public decimal PrincipalCollected     { get; set; }
        public decimal InterestCollected      { get; set; }
        public decimal PenaltiesCollected     { get; set; }
        public int    UniqueBorrowersServed   { get; set; }
        public int    UniqueLoansHandled      { get; set; }
        public decimal AveragePaymentSize     { get; set; }
        public List<CollectorDailyBreakdown> DailyBreakdown { get; set; } = new();
    }

    public class CollectorDailyBreakdown
    {
        public DateTime Date          { get; set; }
        public int    PaymentCount    { get; set; }
        public decimal AmountCollected { get; set; }
    }

    // ─── 4. Borrower Activity Report ──────────────────────────────────────────

    public class BorrowerActivityRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate   { get; set; }
    }

    public class BorrowerActivityResponse
    {
        public BorrowerActivityProfile Profile       { get; set; }
        public BorrowerActivitySummary Summary       { get; set; }
        public List<BorrowerLoanActivity> Loans      { get; set; } = new();
        public List<BorrowerPaymentActivity> Payments { get; set; } = new();
        public List<BorrowerAuditEntry> AuditTrail  { get; set; } = new();
    }

    public class BorrowerActivityProfile
    {
        public int    BorrowerId        { get; set; }
        public string FullName          { get; set; }
        public string PhoneNumber       { get; set; }
        public string Email             { get; set; }
        public string NationalId        { get; set; }
        public string HomeAddress       { get; set; }
        public string EmployerOrBusiness { get; set; }
        public string GuarantorName     { get; set; }
        public string GuarantorPhone    { get; set; }
        public string CreditScore       { get; set; }
        public decimal OnTimePaymentPct { get; set; }
        public bool   IsActive          { get; set; }
        public DateTime MemberSince     { get; set; }
    }

    public class BorrowerActivitySummary
    {
        public int     TotalLoans               { get; set; }
        public int     ActiveLoans              { get; set; }
        public int     ClosedLoans              { get; set; }
        public int     DefaultedLoans           { get; set; }
        public decimal TotalBorrowed            { get; set; }
        public decimal TotalRepaid              { get; set; }
        public decimal TotalOutstanding         { get; set; }
        public decimal TotalInterestPaid        { get; set; }
        public decimal TotalPenaltiesPaid       { get; set; }
        public int     TotalPaymentsMade        { get; set; }
        public int     MissedPayments           { get; set; }
    }

    public class BorrowerLoanActivity
    {
        public int     LoanId             { get; set; }
        public string  LoanNumber         { get; set; }
        public decimal ApprovedAmount     { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string  Status             { get; set; }
        public DateTime? DisbursedOn      { get; set; }
        public DateTime? ClosedOn         { get; set; }
        public int     DurationMonths     { get; set; }
        public decimal Phase1Rate         { get; set; }
        public decimal TotalPaid          { get; set; }
        public decimal TotalInterest      { get; set; }
        public decimal TotalPenalties     { get; set; }
        public string  ApprovedBy         { get; set; }
    }

    public class BorrowerPaymentActivity
    {
        public int     PaymentId       { get; set; }
        public string  PaymentNumber   { get; set; }
        public string  LoanNumber      { get; set; }
        public decimal Amount          { get; set; }
        public decimal PrincipalPortion { get; set; }
        public decimal InterestPortion  { get; set; }
        public decimal PenaltyPortion   { get; set; }
        public string  PaymentMethod    { get; set; }
        public DateTime PaymentDate     { get; set; }
        public string  RecordedBy       { get; set; }
        public string  Notes            { get; set; }
    }

    public class BorrowerAuditEntry
    {
        public string   Action      { get; set; }
        public string   EntityType  { get; set; }
        public int?     EntityId    { get; set; }
        public string   PerformedBy { get; set; }
        public DateTime Timestamp   { get; set; }
        public string   Details     { get; set; }
    }
}
