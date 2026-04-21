using System.Linq.Expressions;
using PRN232_G9_AutoGradingTool.Domain.Common;
using Microsoft.EntityFrameworkCore.Query;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Generic repository with async methods that support cancellation tokens
/// to allow callers to cancel long-running database operations.
/// </summary>
public interface IGenericRepository<T> where T : class, IEntityLike
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    Task<T?> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate, 
        Expression<Func<T, object>>[]? includes = null, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, object>>[]? includes = null, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool isAscending,
        Func<IQueryable<T>, IQueryable<T>>? queryCustomizer,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default);
    
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool isAscending,
        Func<IQueryable<T>, IQueryable<T>>? queryCustomizer,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk delete based on a predicate using EF Core ExecuteDeleteAsync.
    /// </summary>
    Task<int> ExecuteDeleteAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update based on a predicate using EF Core ExecuteUpdateAsync.
    /// Caller provides the SetProperty expression to define the update.
    /// </summary>
    Task<int> ExecuteUpdateAsync(
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    IQueryable<T> GetQueryable();
}