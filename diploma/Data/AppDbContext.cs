using EmployeeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementApp.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }

    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.UserAccounts.Any())
            {
                var admin = new UserAccount
                {
                    Username = "admin",
                    DisplayName = "Администратор",
                    PasswordHash = PasswordHelper.HashPassword("admin123"),
                    AvatarPath = null
                };
                context.UserAccounts.Add(admin);
            }

            if (!context.Employees.Any())
            {
                context.Employees.AddRange(
                    new Employee
                    {
                        FullName = "Иванов Иван Иванович",
                        Position = "Руководитель отдела",
                        Department = "Управление",
                        HireDate = new DateTime(2020, 1, 15),
                        Salary = 120000,
                        BirthDate = new DateTime(1985, 3, 12),
                        Phone = "+7 (900) 000-00-01",
                        Email = "ivanov@example.com",
                        PassportSeries = "1234",
                        PassportNumber = "567890",
                        PassportIssuedBy = "ОВД г. Екатеринбурга",
                        PassportIssueDate = new DateTime(2005, 6, 1),
                        Address = "г. Екатеринбург, ул. Ленина, д. 1",
                        Education = "Высшее, УрФУ, прикладная информатика",
                        Experience = "15 лет управления ИТ-проектами.",
                        DismissalDate = null
                    },
                    new Employee
                    {
                        FullName = "Петров Петр Петрович",
                        Position = "Инженер-программист",
                        Department = "ИТ-отдел",
                        HireDate = new DateTime(2022, 5, 10),
                        Salary = 95000,
                        BirthDate = new DateTime(1998, 11, 5),
                        Phone = "+7 (900) 000-00-02",
                        Email = "petrov@example.com",
                        PassportSeries = "4321",
                        PassportNumber = "098765",
                        PassportIssuedBy = "УФМС г. Екатеринбурга",
                        PassportIssueDate = new DateTime(2016, 9, 1),
                        Address = "г. Екатеринбург, ул. Мира, д. 10",
                        Education = "Высшее, УрФУ, программная инженерия",
                        Experience = "3 года коммерческой разработки на C# и .NET.",
                        DismissalDate = null
                    }
                );
            }

            context.SaveChanges();
        }
    }
}
