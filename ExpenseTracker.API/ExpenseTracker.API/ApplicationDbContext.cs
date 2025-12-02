using Microsoft.EntityFrameworkCore;
namespace ExpenseTracker.API
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Models.Entities.User> Users  => Set<Models.Entities.User>();
        public DbSet<Models.Entities.Expense> Expenses => Set<Models.Entities.Expense>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Models.Entities.User>()
                .HasMany(u => u.Expenses)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.Entities.User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Models.Entities.Expense>()
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");
        }

    }
}
