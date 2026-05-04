using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHS.Models;

namespace QUANLYKHACHS.Data;

public partial class HotelContext : DbContext
{
    public HotelContext()
    {
    }

    public HotelContext(DbContextOptions<HotelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<DepositTransaction> DepositTransactions { get; set; }

    public virtual DbSet<PromoCode> PromoCodes { get; set; }

    public virtual DbSet<PromoCodeUsage> PromoCodeUsages { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomImage> RoomImages { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<Servicess> Servicesses { get; set; }

    public virtual DbSet<SeviceOrder> SeviceOrders { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAvatar> UserAvatars { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AED9F1E4C10");

            entity.Property(e => e.Checkin).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.StatusRoom).HasDefaultValue("active");
            entity.Property(e => e.TotalRoomRice).HasDefaultValue(0m);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Bookings)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Booking_Users");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__Custome__5DCAEF64");

            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__RoomId__5CD6CB2B");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Customerid).HasName("PK__Customer__A4AD58907BACBA19");
        });

        modelBuilder.Entity<DepositTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__DepositT__55433A6B8ECFD6A2");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.User).WithMany(p => p.DepositTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Deposit_Users");
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.HasKey(e => e.PromoId).HasName("PK__PromoCod__33D334B0ABEA06D8");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<PromoCodeUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__PromoCod__29B197205E257A66");

            entity.Property(e => e.UsedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Promo).WithMany(p => p.PromoCodeUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PromoCode__Promo__3587F3E0");

            entity.HasOne(d => d.Transaction).WithMany(p => p.PromoCodeUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PromoCode__Trans__37703C52");

            entity.HasOne(d => d.User).WithMany(p => p.PromoCodeUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PromoCode__UserI__367C1819");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863939881F09D1");

            entity.Property(e => e.TrangThai).HasDefaultValue("Available");

            entity.HasOne(d => d.LoaiPhongNavigation).WithMany(p => p.Rooms).HasConstraintName("FK__Rooms__LoaiPhong__5535A963");
        });

        modelBuilder.Entity<RoomImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__RoomImag__7516F70CD5CEB7BA");

            entity.Property(e => e.IsPrimary).HasDefaultValue(false);
            entity.Property(e => e.UploadedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomImages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RoomImage__RoomI__1CBC4616");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasKey(e => e.RoomTypeId).HasName("PK__RoomType__BCC89631229BABAB");

            entity.Property(e => e.GiaTheoDem).HasDefaultValue(0m);
            entity.Property(e => e.GiaTheoGio).HasDefaultValue(0m);
        });

        modelBuilder.Entity<Servicess>(entity =>
        {
            entity.HasKey(e => e.ServicessId).HasName("PK__Services__A008B2AE5C9372A1");
        });

        modelBuilder.Entity<SeviceOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__SeviceOr__C3905BCF81D221F0");

            entity.Property(e => e.OrderTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Booking).WithMany(p => p.SeviceOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SeviceOrd__Booki__6A30C649");

            entity.HasOne(d => d.Service).WithMany(p => p.SeviceOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SeviceOrd__Servi__6B24EA82");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACEF0173F4");

            entity.Property(e => e.Roles).HasDefaultValue("nv");
        });

        modelBuilder.Entity<UserAvatar>(entity =>
        {
            entity.HasKey(e => e.AvatarId).HasName("PK__UserAvat__4811D66A41AF37CC");

            entity.Property(e => e.UploadedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithOne(p => p.UserAvatar)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserAvata__UserI__17F790F9");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
