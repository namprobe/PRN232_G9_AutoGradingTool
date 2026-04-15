using PRN232_G9_AutoGradingTool.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Context;

public class PRN232_G9_AutoGradingToolDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public PRN232_G9_AutoGradingToolDbContext(DbContextOptions<PRN232_G9_AutoGradingToolDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Đổi tên bảng Identity
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        // AppUser
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.Id).IsUnique();

            // Performance indexes
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JoiningAt);
            entity.HasIndex(x => x.LastLoginAt).HasFilter("\"LastLoginAt\" IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.JoiningAt });
        });

        // AppRole
        builder.Entity<AppRole>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<int>();
        });

        // Configure the base IdentityUserRole<Guid> key
        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
        });

        // Gọi cấu hình chung cho BaseEntity
        BaseEntityConfigurationHelper.ConfigureBaseEntities(builder);
    }
}
