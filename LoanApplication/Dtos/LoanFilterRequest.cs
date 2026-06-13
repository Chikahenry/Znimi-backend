using LoanApplication.Domain.Enums;

namespace LoanApplication.Dtos
{
    public class LoanFilterRequest
    {
        public LoanStatus? Status { get; set; }
        public int? BorrowerId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int? CreatedByUserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "ApplicationDate";
        public bool SortDescending { get; set; } = true;
    }
}
