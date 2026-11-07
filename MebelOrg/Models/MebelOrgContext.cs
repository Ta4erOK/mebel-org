using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MebelOrg.Models;

public partial class MebelOrgContext : DbContext
{
    public MebelOrgContext()
    {
    }

    public MebelOrgContext(DbContextOptions<MebelOrgContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Manufacturer> Manufacturers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PickupPoint> PickupPoints { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(MebelOrg.AppSettings.ConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Categories_Name").IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Manufacturers_Name").IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.PickupPointId, "IX_Orders_PickupPointId");

            entity.HasIndex(e => e.StatusId, "IX_Orders_StatusId");

            entity.HasIndex(e => e.UserId, "IX_Orders_UserId");

            entity.HasIndex(e => e.Number, "UQ_Orders_Number").IsUnique();

            entity.HasOne(d => d.PickupPoint).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PickupPointId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_PickupPoints");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Statuses");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.HasIndex(e => e.ProductId, "IX_OrderItems_ProductId");

            entity.HasIndex(e => new { e.OrderId, e.ProductId }, "UQ_OrderItems_Order_Product").IsUnique();

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<PickupPoint>(entity =>
        {
            entity.HasIndex(e => e.Address, "UQ_PickupPoints_Address").IsUnique();

            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(200);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryId");

            entity.HasIndex(e => e.ManufacturerId, "IX_Products_ManufacturerId");

            entity.HasIndex(e => e.SupplierId, "IX_Products_SupplierId");

            entity.HasIndex(e => e.UnitId, "IX_Products_UnitId");

            entity.HasIndex(e => e.Article, "UQ_Products_Article").IsUnique();

            entity.Property(e => e.Article)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Discount).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ImagePath).HasMaxLength(200);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(300);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");

            entity.HasOne(d => d.Manufacturer).WithMany(p => p.Products)
                .HasForeignKey(d => d.ManufacturerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Manufacturers");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Suppliers");

            entity.HasOne(d => d.Unit).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Units");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Roles_Name").IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Statuses_Name").IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Suppliers_Name").IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Units_Name").IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Login, "UQ_Users_Login").IsUnique();

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.Login)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
