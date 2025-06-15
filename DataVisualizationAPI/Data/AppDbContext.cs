using DataVisualizationAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DataVisualizationAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Dataset configurations
            modelBuilder.Entity<Dataset>()
                .HasIndex(d => d.DatasetName)
                .IsUnique();

            modelBuilder.Entity<DatasetData>()
                .HasIndex(d => d.DatasetId);

            modelBuilder.Entity<DatasetSchema>()
                .HasIndex(d => new { d.DatasetId, d.ColumnName })
                .IsUnique();

            // Configure JSON column
            modelBuilder.Entity<DatasetData>()
                .Property(d => d.RowData)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<Dashboard>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Dashboard>()
                .HasMany(d => d.Items)
                .WithOne(i => i.Dashboard)
                .HasForeignKey(i => i.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Dataset> Datasets { get; set; }
        public DbSet<DatasetSchema> DatasetSchemas { get; set; }
        public DbSet<DatasetData> DatasetData { get; set; }
    }
}
