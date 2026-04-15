namespace PRN232_G9_AutoGradingTool.Domain.Common;

public static class EntityExtension
{
    public static void InitializeEntity(this IEntityLike entity, Guid? userId = null)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.CreatedBy = userId ?? Guid.Empty; //Guid.Empty is for system
        entity.UpdatedBy = userId ?? Guid.Empty;
    }

    public static void UpdateEntity(this IEntityLike entity, Guid? userId = null, IEntityLike? oldEntity = null)
    {
        if (oldEntity != null)
        {
            entity.CreatedAt = oldEntity.CreatedAt;
            entity.CreatedBy = oldEntity.CreatedBy;
        }
        
        entity.UpdatedAt = DateTime.UtcNow;
        if (userId is not null)
        {
            entity.UpdatedBy = userId;
        }
    }

    public static void SoftDeleteEntity(this IEntityLike entity, Guid? userId = null)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = true;
            baseEntity.DeletedAt = DateTime.UtcNow;
            if (userId is not null)
            {
                baseEntity.DeletedBy = userId;
            }
        }
    }

    public static void RestoreEntity(this IEntityLike entity, Guid? userId = null)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = false;
            baseEntity.DeletedAt = null;
            baseEntity.DeletedBy = null;
        }
        entity.UpdatedAt = DateTime.UtcNow;
        if (userId is not null)
        {
            entity.UpdatedBy = userId;
        }
    }
}