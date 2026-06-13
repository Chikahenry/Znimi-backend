namespace LoanApplication.Dtos
{
    public class DailyPayment
    {
        public DateTime Date { get; set; }
        public decimal ExpectedAmount { get; set; }
        public int LoanCount { get; set; }
        public string Status { get; set; } // Paid, Upcoming, Overdue
    }
}
