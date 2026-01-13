using Microsoft.EntityFrameworkCore;
using ThriveStreamController.Data.Entities;

namespace ThriveStreamController.Data
{
    /// <summary>
    /// Database context for the Thrive Stream Controller application.
    /// Manages database connections and entity configurations.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the DbSet for StreamSession entities.
        /// </summary>
        public DbSet<StreamSession> StreamSessions { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for PlatformCredential entities.
        /// </summary>
        public DbSet<PlatformCredential> PlatformCredentials { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for PersistentStreamConfig entities.
        /// </summary>
        public DbSet<PersistentStreamConfig> PersistentStreamConfigs { get; set; }

        /// <summary>
        /// Configures the entity models and their relationships.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure StreamSession entity
            modelBuilder.Entity<StreamSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SceneName).HasMaxLength(200);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });
        }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// Automatically updates CreatedAt and UpdatedAt timestamps.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Asynchronously saves all changes made in this context to the database.
        /// Automatically updates CreatedAt and UpdatedAt timestamps.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the CreatedAt and UpdatedAt timestamps for entities being added or modified.
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is StreamSession && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (StreamSession)entry.Entity;
                entity.UpdatedAt = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}

