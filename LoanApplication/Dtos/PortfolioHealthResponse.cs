namespace LoanApplication.Dtos
{
    public class PortfolioHealthResponse
    {
        public decimal PAR7Percentage { get; set; }
        public decimal PAR30Percentage { get; set; }
        public Dictionary<string, int> LoansByStatus { get; set; }
        public Dictionary<string, int> LoansBySizeBracket { get; set; }
        public List<MonthlyTrendData> TrendData { get; set; }
    }
}
