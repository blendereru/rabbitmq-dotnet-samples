using Microsoft.EntityFrameworkCore;

namespace QuorumQueues.User_Create.Consumer;

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=rabbit-db;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Application Name=\"Microsoft SQL Server Data Tools, T-SQL Editor\"");
    }
}