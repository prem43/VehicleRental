using Microsoft.EntityFrameworkCore;
using VehicleRental.Models.AccountModels;

namespace VehicleRental.Models
{
    public class VehicleRentalDbContext : DbContext
    {
        public VehicleRentalDbContext(DbContextOptions<VehicleRentalDbContext> options) : base(options)
        {
        }

        public DbSet<UserMaster> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserMaster>(entity =>
            {
                entity.HasKey(e => e.U_Id);
                entity.Property(e => e.Full_name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Contact_No).IsRequired().HasMaxLength(12);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(30);
                entity.HasIndex(e => e.RoleName).IsUnique();
            });
        }
    }
}