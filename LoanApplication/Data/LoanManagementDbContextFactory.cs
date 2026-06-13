using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Data
{
    public class LoanManagementDbContextFactory : IDesignTimeDbContextFactory<LoanManagementDbContext>
    {
        public LoanManagementDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<LoanManagementDbContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            return new LoanManagementDbContext(optionsBuilder.Options);
        }
    }
}
