namespace ApiOop.Domain;

public sealed class BankAccount
{
    // Internal state is protected
    private decimal _balance;

    // Immutable identity
    public Guid Id { get; }
    public string OwnerName { get; }

    // Read-only view; write happens only through methods
    public decimal Balance => _balance;

    public BankAccount(Guid id, string ownerName, decimal initialDeposit)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
            throw new ArgumentException("Owner name is required.", nameof(ownerName));
        if (initialDeposit < 0)
            throw new ArgumentOutOfRangeException(nameof(initialDeposit), "Initial deposit cannot be negative.");

        Id = id;
        OwnerName = ownerName.Trim();
        _balance = initialDeposit;
    }

    // Business rules encapsulated
    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Deposit must be positive.");
        _balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Withdrawal must be positive.");
        if (amount > _balance) throw new InvalidOperationException("Insufficient funds.");
        _balance -= amount;
    }
}
