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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        return Ok(list.Select(a => new { a.Id, a.OwnerName, a.Balance }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> Get(Guid id, CancellationToken ct)
    {
        var acc = await _repo.GetAsync(id, ct);
        return acc is null ? NotFound() : Ok(new { acc.Id, acc.OwnerName, acc.Balance });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var acc = new BankAccount(Guid.NewGuid(), request.OwnerName, request.InitialDeposit);
        await _repo.AddAsync(acc, ct);
        return CreatedAtAction(nameof(Get), new { id = acc.Id }, new { acc.Id, acc.OwnerName, acc.Balance });
    }

    [HttpPost("{id:guid}/deposit")]
    public async Task<ActionResult<object>> Deposit(Guid id, DepositRequest req, CancellationToken ct)
    {
        var acc = await _repo.GetAsync(id, ct);
        if (acc is null) return NotFound();

        try
        {
            acc.Deposit(req.Amount);              // encapsulated rule
            await _repo.UpdateAsync(acc, ct);
            return Ok(new { acc.Id, acc.Balance });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<object>> Withdraw(Guid id, WithdrawRequest req, CancellationToken ct)
    {
        var acc = await _repo.GetAsync(id, ct);
        if (acc is null) return NotFound();

        try
        {
            acc.Withdraw(req.Amount);             // encapsulated rule
            await _repo.UpdateAsync(acc, ct);
            return Ok(new { acc.Id, acc.Balance });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
