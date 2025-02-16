using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CsvDataLayer.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=CsvImporter;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            
            optionsBuilder.UseSqlServer(connectionString, options => 
                options.EnableRetryOnFailure());

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}