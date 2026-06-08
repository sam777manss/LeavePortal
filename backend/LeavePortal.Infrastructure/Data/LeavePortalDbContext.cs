using System;
using System.Collections.Generic;
using LeavePortal.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Data;

public partial class LeavePortalDbContext : DbContext
{
    // =====================================================================
    // WHY OnConfiguring() IS NOT HERE
    // =====================================================================
    // When EF Core scaffolds this file, it generates an OnConfiguring()
    // method with the connection string hardcoded inside — including your
    // Azure SQL password in plain text.
    //
    // That is a security risk. If committed to GitHub, your password is public.
    //
    // The correct enterprise approach:
    // - Connection string lives in appsettings.json (LeavePortal.API project)
    // - DbContext is registered in Program.cs using dependency injection:
    //
    //     builder.Services.AddDbContext<LeavePortalDbContext>(options =>
    //         options.UseSqlServer(
    //             builder.Configuration.GetConnectionString("DefaultConnection")));
    //
    // - appsettings.json is added to .gitignore so it is never committed
    //
    // Every time you re-scaffold (schema changes), use this command:
    //   dotnet ef dbcontext scaffold "..." Microsoft.EntityFrameworkCore.SqlServer
    //     --output-dir Entities --context-dir Data --context LeavePortalDbContext
    //     --force --no-onconfiguring
    //     --project LeavePortal.Infrastructure\LeavePortal.Infrastructure.csproj
    //
    // --no-onconfiguring flag prevents scaffold from regenerating OnConfiguring()
    // =====================================================================

    public LeavePortalDbContext(DbContextOptions<LeavePortalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<LeaveApplication> LeaveApplications { get; set; }

    public virtual DbSet<LeaveBalance> LeaveBalances { get; set; }

    public virtual DbSet<LeaveType> LeaveTypes { get; set; }

    public virtual DbSet<NotificationLog> NotificationLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departme__3214EC070715B8F3");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<LeaveApplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LeaveApp__3214EC07888DA746");

            entity.HasIndex(e => e.Status, "IX_LeaveApplications_Status");

            entity.HasIndex(e => e.UserId, "IX_LeaveApplications_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DocumentUrl).HasMaxLength(2000);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ReviewComment).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.LeaveApplications)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveApplications_LeaveTypes");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.LeaveApplicationReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_LeaveApplications_ReviewedBy");

            entity.HasOne(d => d.User).WithMany(p => p.LeaveApplicationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveApplications_Users");
        });

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LeaveBal__3214EC07E3AB67B3");

            entity.HasIndex(e => new { e.UserId, e.Year }, "IX_LeaveBalances_UserId_Year");

            entity.HasIndex(e => new { e.UserId, e.LeaveTypeId, e.Year }, "UQ_LeaveBalances_User_Type_Year").IsUnique();

            entity.Property(e => e.RemainingDays).HasComputedColumnSql("([TotalDays]-[UsedDays])", false);

            entity.HasOne(d => d.LeaveType).WithMany(p => p.LeaveBalances)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveBalances_LeaveTypes");

            entity.HasOne(d => d.User).WithMany(p => p.LeaveBalances)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveBalances_Users");
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LeaveTyp__3214EC0763055B9C");

            entity.HasIndex(e => e.Name, "UQ_LeaveTypes_Name").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07B8C9F27C");

            entity.HasIndex(e => e.LeaveApplicationId, "IX_NotificationLogs_LeaveApplicationId");

            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.RecipientEmail).HasMaxLength(256);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Subject).HasMaxLength(500);

            entity.HasOne(d => d.LeaveApplication).WithMany(p => p.NotificationLogs)
                .HasForeignKey(d => d.LeaveApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationLogs_LeaveApplications");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0785753369");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.Role).HasMaxLength(20);

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Departments");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
