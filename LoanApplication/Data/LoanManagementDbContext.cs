using LoanApplication.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LoanApplication.Data
{
    public class LoanManagementDbContext : DbContext
    {
        public LoanManagementDbContext(DbContextOptions<LoanManagementDbContext> options)
        : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Borrower> Borrowers { get; set; }
        public DbSet<BorrowerDocument> BorrowerDocuments { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<RepaymentSchedule> RepaymentSchedules { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Communication> Communications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

        public DbSet<DailySnapshot> DailySnapshots { get; set; }
        public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; }
        public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Borrower Configuration
            modelBuilder.Entity<Borrower>()
                .HasIndex(b => b.NationalIdNumber)
                .IsUnique();

            modelBuilder.Entity<Borrower>()
                .HasIndex(b => b.PhoneNumber);

            // Loan Configuration
            modelBuilder.Entity<Loan>()
                .HasIndex(l => l.LoanNumber)
                .IsUnique();

            modelBuilder.Entity<Loan>()
                .HasIndex(l => l.Status);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Borrower)
                .WithMany(b => b.Loans)
                .HasForeignKey(l => l.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.CreatedBy)
                .WithMany(u => u.CreatedLoans)
                .HasForeignKey(l => l.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment Configuration
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentNumber)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Loan)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserActivityLog Configuration
            modelBuilder.Entity<UserActivityLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<UserActivityLog>()
                .HasIndex(a => a.CreatedAt);

            // System Configuration
            modelBuilder.Entity<SystemConfiguration>()
                .HasIndex(sc => sc.ConfigKey)
                .IsUnique();
        }


    }
}
