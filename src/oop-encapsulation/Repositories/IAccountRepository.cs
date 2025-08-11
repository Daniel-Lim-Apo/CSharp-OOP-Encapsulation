using ApiOop.Domain;

namespace ApiOop.Repositories;

public interface IAccountRepository
{
    Task<BankAccount?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BankAccount account, CancellationToken ct = default);
    Task UpdateAsync(BankAccount account, CancellationToken ct = default);
    Task<IReadOnlyList<BankAccount>> ListAsync(CancellationToken ct = default);
}
