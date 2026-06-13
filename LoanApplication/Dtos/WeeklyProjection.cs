namespace LoanApplication.Dtos
{
    public class WeeklyProjection
    {
        public string WeekRange { get; set; }
        public List<DailyPayment> DailyPayments { get; set; }
        public decimal TotalExpected { get; set; }
    }
}
