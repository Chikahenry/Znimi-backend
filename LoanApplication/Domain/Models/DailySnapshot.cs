using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Domain.Models
{
    public class DailySnapshot
    {
        [Key]
        public int SnapshotId { get; set; }

        public DateTime SnapshotDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalLoansOutstanding { get; set; }

        public int ActiveLoansCount { get; set; }

        public int OverdueLoansCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InterestIncomeMonthToDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PAR7Percentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PAR30Percentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CollectionRate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
