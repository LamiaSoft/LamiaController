using LamiaController.Models;
using Microsoft.EntityFrameworkCore;

namespace LamiaController {

  public class PlatformContext : DbContext {
    
    public DbSet<Account> accounts { get; set; }
    public DbSet<AccountType> accountTypes { get; set; }

    protected static string connectionString;

    public PlatformContext(string dBConnectionString) : base(GetDbContextOptions(dBConnectionString)) {
      connectionString = dBConnectionString;
    }

    public static string GetConnectionString() {
      return connectionString;
    }

    private static DbContextOptions GetDbContextOptions(string dBConnectionString) {
      return new DbContextOptionsBuilder().UseMySQL(dBConnectionString).Options;
    }

  }

}
