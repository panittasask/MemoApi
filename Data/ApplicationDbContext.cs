using MemmoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }
        public DbSet<SettingParent> SettingParents { get; set; }
        public DbSet<SettingChild> SettingChildren { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<WorkflowNode> WorkflowNodes { get; set; }
        public DbSet<WorkflowEdge> WorkflowEdges { get; set; }
        public DbSet<WorkNote> WorkNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SettingParent>()
                .HasMany(x => x.Children)
                .WithOne(x => x.Parent)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowNode>()
                .HasMany(n => n.OutgoingEdges)
                .WithOne(e => e.FromNode)
                .HasForeignKey(e => e.FromNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkflowNode>()
                .HasMany(n => n.IncomingEdges)
                .WithOne(e => e.ToNode)
                .HasForeignKey(e => e.ToNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            var seedDate = new DateTime(2026, 3, 25);
            const string statusParentId = "SET_PARENT_STATUS";
            const string projectParentId = "SET_PARENT_PROJECT";

            modelBuilder.Entity<SettingParent>().HasData(
                new SettingParent
                {
                    Id = statusParentId,
                    Key = "status",
                    Name = "Status",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingParent
                {
                    Id = projectParentId,
                    Key = "project",
                    Name = "Project",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                }
            );

            modelBuilder.Entity<SettingChild>().HasData(
                new SettingChild
                {
                    Id = "SET_CHILD_STATUS_TODO",
                    ParentId = statusParentId,
                    Key = "todo",
                    Name = "To Do",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingChild
                {
                    Id = "SET_CHILD_STATUS_INPROGRESS",
                    ParentId = statusParentId,
                    Key = "inprogress",
                    Name = "In Progress",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingChild
                {
                    Id = "SET_CHILD_STATUS_DONE",
                    ParentId = statusParentId,
                    Key = "done",
                    Name = "Done",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingChild
                {
                    Id = "SET_CHILD_STATUS_BLOCKED",
                    ParentId = statusParentId,
                    Key = "blocked",
                    Name = "Blocked",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingChild
                {
                    Id = "SET_CHILD_PROJECT_INTERNAL",
                    ParentId = projectParentId,
                    Key = "internal",
                    Name = "Internal",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingChild
                {
                    Id = "SET_CHILD_PROJECT_CLIENT",
                    ParentId = projectParentId,
                    Key = "client",
                    Name = "Client",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingChild
                {
                    Id = "SET_CHILD_PROJECT_MAINTENANCE",
                    ParentId = projectParentId,
                    Key = "maintenance",
                    Name = "Maintenance",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                },
                new SettingChild
                {
                    Id = "SET_CHILD_PROJECT_RESEARCH",
                    ParentId = projectParentId,
                    Key = "research",
                    Name = "Research",
                    CreatedDate = seedDate,
                    UpdateDate = seedDate
                }
            );
        }

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