using LoanApplication.Domain.Enums;

namespace LoanApplication.Dtos
{
    public class LoanDetailsResponse
    {
        public int LoanId { get; set; }
        public string LoanNumber { get; set; }
        public string BorrowerName { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public decimal TotalInterestAccrued { get; set; }
        public decimal TotalPenaltiesAccrued { get; set; }
        public LoanStatus Status { get; set; }
        public DateTimeOffset? DisbursementDate { get; set; }
        public DateTimeOffset? FirstPaymentDueDate { get; set; }
        public List<RepaymentScheduleDto> RepaymentSchedule { get; set; }
        public List<PaymentHistoryDto> PaymentHistory { get; set; }
    }
}
