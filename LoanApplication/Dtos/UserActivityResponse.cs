namespace LoanApplication.Dtos
{
    public class UserActivityResponse
    {
        public int ActivityId { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
