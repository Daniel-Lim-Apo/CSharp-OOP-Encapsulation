# CSharp-OOP-Encapsulation

A beginner-friendly ASP.NET Core Web API that demonstrates **Encapsulation** — one of the four pillars of Object-Oriented Programming (OOP) — using a simple Bank Account example.

---

## 🏛️ What is Encapsulation?

**Encapsulation** means **hiding the internal details of an object** and only exposing what is necessary to the outside world.

Think of it like a **vending machine**:
- You press a button (public interface) to get a snack.
- You **cannot** reach inside and grab cash from the coin mechanism (private internals).
- The machine enforces its own rules: you must insert enough money before the snack is released.

In C#, encapsulation is achieved using:
| Feature | Purpose |
|---|---|
| `private` fields | Hide internal state |
| `public` properties | Expose read-only views of state |
| `public` methods | Expose controlled operations with business rules |
| `sealed` classes | Prevent unintended inheritance |

---

## 📁 Project Structure

```
src/oop-encapsulation/
├── Domain/
│   └── BankAccount.cs             # The core encapsulated entity
├── Contracts/
│   ├── CreateAccountRequest.cs    # Input DTO for account creation
│   ├── DepositRequest.cs          # Input DTO for deposits
│   └── WithdrawRequest.cs         # Input DTO for withdrawals
├── Repositories/
│   ├── IAccountRepository.cs      # Contract (interface) for storage
│   └── InMemoryAccountRepository.cs # In-memory implementation
├── Controllers/
│   └── AccountsController.cs      # HTTP API layer
├── Program.cs                     # App startup / dependency injection
└── BankAccountTests.http          # Manual HTTP test scripts
```

---

## 🏦 1. The Core Domain — `BankAccount.cs`

This is the heart of the example. Everything else exists to serve this class.

```csharp
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
```

### Line-by-line breakdown

#### `public sealed class BankAccount`
- `public` — the class can be used from anywhere.
- `sealed` — **no other class can inherit from it**. This is good encapsulation practice: it prevents someone from accidentally overriding behaviour that might break the business rules you've carefully built in.

#### `private decimal _balance;`
- The `_balance` field is `private`. That means **no code outside this class can ever set it directly**.
- Without encapsulation someone could do `account._balance = -99999;`, bypassing all rules. The `private` keyword makes that impossible.
- The underscore prefix (`_balance`) is a C# naming convention for private fields.

#### `public Guid Id { get; }` and `public string OwnerName { get; }`
- These are **read-only auto-properties**. They have only a getter, no setter.
- Once set in the constructor, they can **never be changed**. This makes the identity of an account immutable and trustworthy.

#### `public decimal Balance => _balance;`
- This is an **expression-bodied read-only property**.
- It lets the outside world *read* the balance, but they cannot *write* to it.
- The `=> _balance` syntax is shorthand for `get { return _balance; }`.

#### The Constructor
```csharp
public BankAccount(Guid id, string ownerName, decimal initialDeposit)
```
- The constructor is the **only way to create a valid `BankAccount`**.
- It validates all inputs up-front, so an invalid account object can never exist.
  - Empty owner name → `ArgumentException`
  - Negative initial deposit → `ArgumentOutOfRangeException`
- This is called **"defensive programming"** — the object protects itself from bad data.

#### `public void Deposit(decimal amount)` and `public void Withdraw(decimal amount)`
- These are the **only ways to change the balance**.
- Each method enforces its own business rules before modifying `_balance`:
  - `Deposit`: amount must be positive.
  - `Withdraw`: amount must be positive AND must not exceed the current balance.
- If a rule is violated, an `InvalidOperationException` is thrown. The balance is **never** touched.

> **Why does this matter?** Without these methods, any external code could do `account._balance -= 1000000` without any checks. Encapsulation prevents this entirely.

---

## 📨 2. Contracts (Data Transfer Objects) — `Contracts/`

DTOs (Data Transfer Objects) are simple objects used to carry data *into* the API from an HTTP request. They do **not** contain any business logic.

### `CreateAccountRequest.cs`
```csharp
namespace ApiOop.Contracts;

public sealed record CreateAccountRequest(string OwnerName, decimal InitialDeposit);
```
A `record` in C# is a lightweight, immutable data type. It automatically generates:
- A constructor: `new CreateAccountRequest("Alice", 100m)`
- Equality comparison
- A `ToString()` representation

This record carries the two pieces of information needed to open a new account.

### `DepositRequest.cs`
```csharp
public sealed record DepositRequest(decimal Amount);
```
Carries a single `Amount` value for a deposit operation.

### `WithdrawRequest.cs`
```csharp
public sealed record WithdrawRequest(decimal Amount);
```
Carries a single `Amount` value for a withdrawal operation.

> **Note:** These records represent the *shape* of data that clients send. Keeping them separate from the `BankAccount` domain class is itself a form of encapsulation — the API's public "contract" is decoupled from the internal domain model.

---

## 🗄️ 3. Repository Pattern — `Repositories/`

The **Repository Pattern** is a design pattern that encapsulates all data access logic. The rest of the application doesn't need to know *how* or *where* accounts are stored.

### `IAccountRepository.cs`
```csharp
using ApiOop.Domain;

namespace ApiOop.Repositories;

public interface IAccountRepository
{
    Task<BankAccount?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BankAccount account, CancellationToken ct = default);
    Task UpdateAsync(BankAccount account, CancellationToken ct = default);
    Task<IReadOnlyList<BankAccount>> ListAsync(CancellationToken ct = default);
}
```

- An `interface` in C# defines a **contract**: a list of methods that any implementing class *must* provide.
- It **hides implementation details** — the controller only knows about this interface, not *how* data is stored.
- `Task<T>` makes each method asynchronous (non-blocking), which is standard in ASP.NET Core.
- `BankAccount?` — the `?` means the return value can be `null` (account not found).
- `IReadOnlyList<BankAccount>` — the caller gets a read-only collection; they cannot add or remove items from the list.

### `InMemoryAccountRepository.cs`
```csharp
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
```

- `InMemoryAccountRepository` **implements** `IAccountRepository` — it provides the real code for all four methods.
- `ConcurrentDictionary<Guid, BankAccount> _store` — a thread-safe dictionary that maps account IDs to account objects. It's `private` so nothing outside this class can touch it.
- `_store[account.Id] = account` — stores or overwrites an account by its ID.
- `Task.CompletedTask` — since this is in-memory (instant), we return a completed task with no result.
- `Task.FromResult(...)` — wraps a synchronous value inside a completed `Task`, satisfying the async interface.

> **The key encapsulation benefit:** If you later replace this with a database-backed repository, **none of the controller or domain code changes** — only this class is swapped out.

---

## 🌐 4. API Controller — `AccountsController.cs`

The controller is the **HTTP layer**. It receives HTTP requests, calls the domain/repository, and returns HTTP responses.

```csharp
using ApiOop.Contracts;
using ApiOop.Domain;
using ApiOop.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ApiOop.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountRepository _repo;

    public AccountsController(IAccountRepository repo) => _repo = repo;
    ...
}
```

- `[ApiController]` — tells ASP.NET Core this is an API controller (enables automatic model validation and more).
- `[Route("api/[controller]")]` — maps HTTP requests to `api/accounts` (the `[controller]` token is replaced with the class name minus "Controller").
- `private readonly IAccountRepository _repo` — the controller depends on the **interface**, not the concrete class. This is **Dependency Injection** working in harmony with encapsulation.
- The constructor receives the repository and stores it. `=> _repo = repo` is shorthand for a one-liner constructor body.

### Endpoints

#### `GET /api/accounts` — List all accounts
```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<object>>> List(CancellationToken ct)
{
    var list = await _repo.ListAsync(ct);
    return Ok(list.Select(a => new { a.Id, a.OwnerName, a.Balance }));
}
```
Returns a JSON array of all accounts. Notice it projects only `Id`, `OwnerName`, and `Balance` — it does **not** leak internal fields like `_balance`.

#### `GET /api/accounts/{id}` — Get one account
```csharp
[HttpGet("{id:guid}")]
public async Task<ActionResult<object>> Get(Guid id, CancellationToken ct)
{
    var acc = await _repo.GetAsync(id, ct);
    return acc is null ? NotFound() : Ok(new { acc.Id, acc.OwnerName, acc.Balance });
}
```
- `{id:guid}` — route constraint ensuring the `id` segment must be a valid GUID.
- Returns `404 Not Found` if no account exists with that ID.

#### `POST /api/accounts` — Create an account
```csharp
[HttpPost]
public async Task<ActionResult<object>> Create(CreateAccountRequest request, CancellationToken ct)
{
    var acc = new BankAccount(Guid.NewGuid(), request.OwnerName, request.InitialDeposit);
    await _repo.AddAsync(acc, ct);
    return CreatedAtAction(nameof(Get), new { id = acc.Id }, new { acc.Id, acc.OwnerName, acc.Balance });
}
```
- Creates a new `BankAccount` via its constructor (which validates the data).
- Returns `201 Created` with a `Location` header pointing to the new resource.

#### `POST /api/accounts/{id}/deposit` — Deposit money
```csharp
[HttpPost("{id:guid}/deposit")]
public async Task<ActionResult<object>> Deposit(Guid id, DepositRequest req, CancellationToken ct)
{
    var acc = await _repo.GetAsync(id, ct);
    if (acc is null) return NotFound();

    try
    {
        acc.Deposit(req.Amount);      // encapsulated rule
        await _repo.UpdateAsync(acc, ct);
        return Ok(new { acc.Id, acc.Balance });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
```
- The controller **delegates** the business rule enforcement to `acc.Deposit(...)`.
- If `Deposit` throws (e.g. zero amount), it is caught and returned as `400 Bad Request`.
- This is encapsulation in action: the controller doesn't know *how* deposit works — it just calls the method.

#### `POST /api/accounts/{id}/withdraw` — Withdraw money
```csharp
[HttpPost("{id:guid}/withdraw")]
public async Task<ActionResult<object>> Withdraw(Guid id, WithdrawRequest req, CancellationToken ct)
{
    var acc = await _repo.GetAsync(id, ct);
    if (acc is null) return NotFound();

    try
    {
        acc.Withdraw(req.Amount);     // encapsulated rule
        await _repo.UpdateAsync(acc, ct);
        return Ok(new { acc.Id, acc.Balance });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
```
Same pattern as deposit. Business logic lives in the domain class; the controller just orchestrates.

---

## ⚙️ 5. Program Startup — `Program.cs`

```csharp
using ApiOop.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Demo repository (in-memory)
builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
```

- `WebApplication.CreateBuilder(args)` — bootstraps the ASP.NET Core application.
- `AddControllers()` — registers MVC controllers (including `AccountsController`).
- `AddSwaggerGen()` + `UseSwagger()` + `UseSwaggerUI()` — generates interactive API documentation at `/swagger`.
- `AddSingleton<IAccountRepository, InMemoryAccountRepository>()` — this is **Dependency Injection**:
  - Whenever ASP.NET Core needs an `IAccountRepository`, it will provide an `InMemoryAccountRepository`.
  - `AddSingleton` means only **one instance** is created and shared for the lifetime of the app (so data persists in memory between requests).
- `MapControllers()` — maps HTTP routes to controller actions.

---

## 🧪 6. HTTP Test Script — `BankAccountTests.http`

This file lets you manually test all API endpoints using the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension for VS Code, or JetBrains HTTP Client.

```http
@baseUrl = http://localhost:5287

### Create account
POST {{baseUrl}}/api/accounts
Content-Type: application/json

{
  "ownerName": "Anna Smith",
  "initialDeposit": 100.00
}

### Deposit money
POST {{baseUrl}}/api/accounts/{{createAccount.response.body.$.id}}/deposit
Content-Type: application/json

{ "amount": 50.00 }

### Negative test: withdraw more than balance (should return 400)
POST {{baseUrl}}/api/accounts/{{createAccount.response.body.$.id}}/withdraw
Content-Type: application/json

{ "amount": 999999 }
```

- `@baseUrl` — a variable reused throughout the file.
- `{{createAccount.response.body.$.id}}` — dynamically captures the `id` from the create response.
- The negative tests confirm that the encapsulated business rules in `BankAccount` correctly return `400 Bad Request` with a descriptive error message.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later

### Run the application
```bash
cd src/oop-encapsulation
dotnet run
```

The API will be available at `http://localhost:5287`.  
Open `http://localhost:5287/swagger` in your browser to explore and test all endpoints interactively.

### Create the project from scratch
```bash
dotnet new webapi --use-controllers -o oop-encapsulation
```

---

## 💡 Key Encapsulation Takeaways

| Concept | Where it appears in this project |
|---|---|
| `private` field | `_balance` in `BankAccount` |
| Read-only property | `Balance`, `Id`, `OwnerName` in `BankAccount` |
| Validation in constructor | `BankAccount` constructor checks for empty names and negative deposits |
| Rules in methods | `Deposit` and `Withdraw` enforce business invariants |
| `sealed` class | `BankAccount`, DTOs, and both repository classes |
| Interface abstraction | `IAccountRepository` hides storage implementation details |
| Dependency Injection | Controller depends on `IAccountRepository`, not the concrete class |

> **Remember:** Encapsulation is not just `private` vs `public`. It's about designing objects that are **responsible for their own state and rules**, making your code safer, more maintainable, and easier to understand.

---

## 📄 License

MIT
