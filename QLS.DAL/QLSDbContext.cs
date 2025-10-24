using QLS.DAL.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace QLS.DAL
{
    public class QLSDbContext : DbContext
    {
        public QLSDbContext() : base("QLSDbContext") { }

        public DbSet<LoaiSach> LoaiSaches { get; set; }
        public DbSet<Sach> Saches { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Không pluralize tên bảng
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // MaSach là CHAR(6) cố định
            modelBuilder.Entity<Sach>()
                        .Property(s => s.MaSach)
                        .IsFixedLength()
                        .HasMaxLength(6);

            modelBuilder.Entity<Sach>()
                        .HasRequired(s => s.Loai)
                        .WithMany(l => l.Saches)
                        .HasForeignKey(s => s.MaLoai)
                        .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
