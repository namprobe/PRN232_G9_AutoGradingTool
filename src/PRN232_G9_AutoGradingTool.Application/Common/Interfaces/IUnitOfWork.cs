using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class, IEntityLike;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL command (INSERT/UPDATE/DELETE or stored procedure)
    /// without exposing DbContext to the outside.
    /// Returns the number of affected rows.
    /// </summary>
    Task<int> ExecuteSqlRawAsync(
        string sql,
        object[]? parameters = null,
        CancellationToken cancellationToken = default);

    void Dispose();
}