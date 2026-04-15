using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Identity;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private IDbContextTransaction? _transaction;
    private readonly Dictionary<Type, object> _repositories = new();
    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IEntityLike
    {
        if (!_repositories.ContainsKey(typeof(T)))
        {
            var repo = new GenericRepository<T>(_context);
            _repositories[typeof(T)] = repo;
        }
        return (IGenericRepository<T>)_repositories[typeof(T)];
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteSqlRawAsync(
        string sql,
        object[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL command must not be null or empty.", nameof(sql));

        return parameters is { Length: > 0 }
            ? await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken)
            : await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}