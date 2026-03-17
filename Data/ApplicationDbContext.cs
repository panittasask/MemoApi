using MemmoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAbstractions();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyAbstractions();
            return base.SaveChanges();
        }

        private void ApplyAbstractions()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added ||
                        e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (BaseEntity)entityEntry.Entity;

                entity.UpdateDate = DateTime.Now;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedDate = DateTime.Now;
                }
            }
        }
    }
}