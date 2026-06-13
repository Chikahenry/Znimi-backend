using LoanApplication.Domain.Enums;

namespace LoanApplication.Dtos
{
    public class BorrowerFilterRequest
    {
        public int? LoanId { get; set; }
        public int? BorrowerId { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public CreditScore? CreditScore { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
