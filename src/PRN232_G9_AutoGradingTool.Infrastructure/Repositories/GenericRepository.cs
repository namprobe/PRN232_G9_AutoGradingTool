using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation with cancellation token support
/// for all async database operations.
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntityLike
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    private void DetachEntity(T entity)
    {
        var entityType = typeof(T);
        // Look for Id property (int) or any property ending with "Id" that's a Guid
        var keyProperty = entityType.GetProperties()
            .FirstOrDefault(p => (p.Name == "Id" && (p.PropertyType == typeof(int) || p.PropertyType == typeof(Guid))) ||
                                (p.Name.EndsWith("Id") && p.PropertyType == typeof(Guid)));

        if (keyProperty != null)
        {
            var entityKeyValue = keyProperty.GetValue(entity);

            // Only detach if the key has a valid value (not 0 for int, not empty Guid)
            bool hasValidKey = false;
            if (keyProperty.PropertyType == typeof(int))
            {
                hasValidKey = entityKeyValue != null && (int)entityKeyValue != 0;
            }
            else if (keyProperty.PropertyType == typeof(Guid))
            {
                hasValidKey = entityKeyValue != null && (Guid)entityKeyValue != Guid.Empty;
            }

            if (hasValidKey)
            {
                //Find and detach any existing entity with the same key
                var existingEntry = _context.ChangeTracker.Entries<T>()
                    .FirstOrDefault(e => e.Entity != entity &&
                    keyProperty.GetValue(e.Entity)?.Equals(entityKeyValue) == true);

                if (existingEntry != null)
                {
                    _context.Entry(existingEntry.Entity).State = EntityState.Detached;
                }
            }
        }
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        return await query.AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        return await query.CountAsync(cancellationToken);
    }

    public virtual void Delete(T entity)
    {
        // Kiểm tra entity đã được track chưa
        var entry = _context.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity == entity);
        if (entry == null)
        {
            // Nếu chưa track thì detach entity và attach vào context
            DetachEntity(entity);
            _dbSet.Attach(entity);
        }
        else
        {
            // Nếu đã track thì chỉ cần set state là deleted
            entry.State = EntityState.Deleted;
        }
        // Remove entity
        _dbSet.Remove(entity);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            // Check if entity is already tracked
            var entry = _context.ChangeTracker.Entries<T>()
                .FirstOrDefault(e => e.Entity == entity);

            if (entry == null)
            {
                // Not tracked - attach first
                // No need to detach since entities come from AsNoTracking query
                _dbSet.Attach(entity);
            }
        }

        // Batch remove all entities
        _dbSet.RemoveRange(entities);
    }

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool isAscending,
        Func<IQueryable<T>, IQueryable<T>>? queryCustomizer,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

        if (queryCustomizer != null)
        {
            query = queryCustomizer(query);
        }

        if (orderBy != null)
        {
            query = isAscending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<T?> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }


    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool isAscending,
        Func<IQueryable<T>, IQueryable<T>>? queryCustomizer,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

        if (queryCustomizer != null)
        {
            query = queryCustomizer(query);
        }

        if (orderBy != null)
        {
            query = isAscending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public IQueryable<T> GetQueryable()
    {
        return _dbSet.AsNoTracking();
    }

    public virtual void Update(T entity)
    {
        // Kiểm tra entity đã được track chưa
        var entry = _context.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity == entity);
        if (entry != null)
        {
            // Nếu đã track thì chỉ cần set state là modified
            entry.State = EntityState.Modified;
        }
        else
        {
            // Detach entity nếu đã tồn tại trong context
            DetachEntity(entity);
            // Attach entity vào context
            _dbSet.Attach(entity);
            // Set state là modified
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();

        foreach (var entity in entityList)
        {
            // Check if entity is already tracked
            var entry = _context.ChangeTracker.Entries<T>()
                .FirstOrDefault(e => e.Entity == entity);

            if (entry != null)
            {
                // Already tracked - just set state
                entry.State = EntityState.Modified;
            }
            else
            {
                // Not tracked - detach any existing entity with same key, then attach
                DetachEntity(entity);
                _dbSet.Attach(entity);
                var newEntry = _context.Entry(entity);
                newEntry.State = EntityState.Modified;
            }
        }
    }

    /// <summary>
    /// Bulk delete using EF Core ExecuteDeleteAsync based on a predicate.
    /// </summary>
    public async Task<int> ExecuteDeleteAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return await _dbSet
            .Where(predicate)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Bulk update using EF Core ExecuteUpdateAsync based on a predicate and set-properties expression.
    /// </summary>
    public async Task<int> ExecuteUpdateAsync(
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (setProperties == null) throw new ArgumentNullException(nameof(setProperties));

        IQueryable<T> query = _dbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ExecuteUpdateAsync(setProperties, cancellationToken);
    }
}