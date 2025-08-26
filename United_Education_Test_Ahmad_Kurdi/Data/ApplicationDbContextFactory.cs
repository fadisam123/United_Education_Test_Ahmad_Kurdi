using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace United_Education_Test_Ahmad_Kurdi.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddDbContext<AppDbContext>(options =>
                    //options.UseSqlServer("Server=DESKTOP-J8QF0MB\\SQLEXPRESS;Database=UnitedEducationDB;Trusted_Connection=True;TrustServerCertificate=True;Integrated Security=False;Encrypt=False;MultipleActiveResultSets=true;"))
                    options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=UnitedEducationDB;Trusted_Connection=True;"))
                .BuildServiceProvider();

            return serviceProvider.GetRequiredService<AppDbContext>();
        }
    }
}
