using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using System.Linq.Expressions;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Context;

/// <summary>
/// Static helper for shared entity configuration patterns
/// </summary>
public static class BaseEntityConfigurationHelper
{
    /// <summary>
    /// Configure common patterns for all BaseEntity-derived entities
    /// </summary>
    public static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter only to root entities inheriting from BaseEntity
        // In inheritance scenarios, filters should only be applied to the root type
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.BaseType == null)
            {
                // Apply soft delete filter only to root entity types
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var falseConstant = Expression.Constant(false);
                var condition = Expression.Equal(property, falseConstant);
                var lambda = Expression.Lambda(condition, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Configure common BaseEntity properties for all derived entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            ConfigureBaseEntityProperties(modelBuilder, entityType.ClrType);
        }
    }

    /// <summary>
    /// Configure standard BaseEntity properties
    /// </summary>
    private static void ConfigureBaseEntityProperties(ModelBuilder modelBuilder, Type entityType)
    {
        // Only configure primary key on root entity types, not derived types
        // This is crucial for Entity Framework inheritance scenarios
        var entityTypeInfo = modelBuilder.Model.FindEntityType(entityType);
        if (entityTypeInfo?.BaseType == null)
        {
            // This is a root entity type, configure the primary key
            modelBuilder.Entity(entityType).HasKey("Id");
        }
        
        // Default values for audit fields
        modelBuilder.Entity(entityType)
            .Property<DateTime?>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("timestamp with time zone");
            
        modelBuilder.Entity(entityType)
            .Property<DateTime?>("UpdatedAt")
            .HasColumnType("timestamp with time zone");
        
        modelBuilder.Entity(entityType)
            .Property<DateTime?>("DeletedAt")
            .HasColumnType("timestamp with time zone");
            
        modelBuilder.Entity(entityType)
            .Property<bool>("IsDeleted")
            .HasDefaultValue(false);

        // Enum conversions
        modelBuilder.Entity(entityType)
            .Property<EntityStatusEnum>("Status")
            .HasConversion<int>();

        // CRITICAL: Index on IsDeleted for Global Query Filter Performance
        // Global filter applies "WHERE is_deleted = false" on all queries
        // This index significantly improves query performance
        modelBuilder.Entity(entityType)
            .HasIndex("IsDeleted")
            .HasDatabaseName($"ix_{entityType.Name.ToLower()}_is_deleted");

        // Index on Status for common filtering
        modelBuilder.Entity(entityType)
            .HasIndex("Status")
            .HasDatabaseName($"ix_{entityType.Name.ToLower()}_status");

        // Composite index for Status + IsDeleted (common query pattern)
        modelBuilder.Entity(entityType)
            .HasIndex("Status", "IsDeleted")
            .HasDatabaseName($"ix_{entityType.Name.ToLower()}_status_is_deleted");

        // IMPORTANT: Index on UpdatedAt for "Recently Modified" queries
        // Most list views show recently updated items, not just newly created
        // This is more relevant than CreatedAt for operational dashboards
        modelBuilder.Entity(entityType)
            .HasIndex("UpdatedAt")
            .HasDatabaseName($"ix_{entityType.Name.ToLower()}_updated_at");

        // Composite index for recent active items (very common query pattern)
        // Usage: Get active non-deleted items sorted by last update
        modelBuilder.Entity(entityType)
            .HasIndex("IsDeleted", "Status", "UpdatedAt")
            .HasDatabaseName($"ix_{entityType.Name.ToLower()}_is_deleted_status_updated_at");
    }
}
