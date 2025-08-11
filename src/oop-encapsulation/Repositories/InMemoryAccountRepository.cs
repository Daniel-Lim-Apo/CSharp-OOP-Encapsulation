using ApiOop.Domain;
using System.Collections.Concurrent;

namespace ApiOop.Repositories;

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<Guid, BankAccount> _store = new();

    public Task AddAsync(BankAccount account, CancellationToken ct = default)
    {
        _store[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task<BankAccount?> GetAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var a) ? a : null);

    public Task UpdateAsync(BankAccount account, CancellationToken ct = default)
    {
        _store[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BankAccount>> ListAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<BankAccount>)_store.Values.ToList());
}
