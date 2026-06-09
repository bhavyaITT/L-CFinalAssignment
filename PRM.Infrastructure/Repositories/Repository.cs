using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using global::PRM.Application.Interfaces.Repository;
using global::PRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using PRM.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace PRM.Infrastructure.Repositories
{
    /// <summary>
    /// Concrete implementation of IRepository using EF Core.
    /// Application layer never imports this class — it only knows the interface.
    /// Swapping EF Core for Dapper later would only touch this file.
    /// </summary>
    public class Repository<T>(PRMTDbContext context) : IRepository<T> where T : BaseEntity
    {
        private readonly DbSet<T> _set = context.Set<T>();

        public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
            await _set.FindAsync([id], ct);

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
            await _set.ToListAsync(ct);

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await _set.Where(predicate).ToListAsync(ct);

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await _set.FirstOrDefaultAsync(predicate, ct);

        public async Task AddAsync(T entity, CancellationToken ct = default) =>
            await _set.AddAsync(entity, ct);

        public void Update(T entity) =>
            _set.Update(entity);

        public void Remove(T entity) =>
            _set.Remove(entity);

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await _set.AnyAsync(predicate, ct);

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await _set.CountAsync(predicate, ct);
    }
}
