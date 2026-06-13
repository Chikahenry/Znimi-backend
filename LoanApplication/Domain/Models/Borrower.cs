using LoanApplication.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoanApplication.Domain.Models
{
    public class Borrower
    {
        [Key]
        public int BorrowerId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(20)]
        public string AlternatePhoneNumber { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        [Required, MaxLength(50)]
        public string NationalIdNumber { get; set; }

        [MaxLength(200)]
        public string HomeAddress { get; set; }

        [MaxLength(100)]
        public string EmployerOrBusiness { get; set; }

        [MaxLength(100)]
        public string GuarantorName { get; set; }

        [MaxLength(20)]
        public string GuarantorPhone { get; set; }

        public CreditScore InternalCreditScore { get; set; } = CreditScore.B;

        public decimal OnTimePaymentPercentage { get; set; } = 100m;

        public int TotalLoansCount { get; set; } = 0;

        public int DefaultedLoansCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LastUpdatedAt { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<Loan> Loans { get; set; }
        [JsonIgnore]
        public ICollection<BorrowerDocument> Documents { get; set; }
        [JsonIgnore]
        public ICollection<Communication> Communications { get; set; }
    }
}
