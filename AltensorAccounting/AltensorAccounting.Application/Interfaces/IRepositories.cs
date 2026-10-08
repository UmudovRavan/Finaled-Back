using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Domain.Common;

namespace AltensorAccounting.Application.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();
    IQueryable<T> QueryIgnoreFilters();
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default, params Expression<Func<T, object>>[] includes);
    Task<T?> GetByIdIgnoreFiltersAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
