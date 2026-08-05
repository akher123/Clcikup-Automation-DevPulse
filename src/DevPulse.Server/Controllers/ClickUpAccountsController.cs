using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Shared.Contracts.ClickUp;
using Microsoft.AspNetCore.Mvc;

namespace DevPulse.Server.Controllers;

[ApiController]
[Route("api/clickup/accounts")]
public sealed class ClickUpAccountsController : ControllerBase
{
    private readonly IClickUpAccountService _accountService;

    public ClickUpAccountsController(IClickUpAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClickUpAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAsync(cancellationToken);
        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClickUpAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClickUpAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateClickUpAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountService.CreateAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error, errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClickUpAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClickUpAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountService.UpdateAsync(id, request, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error, errors = result.Errors });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _accountService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpGet("{id:guid}/test")]
    [ProducesResponseType(typeof(ClickUpConnectionTestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken cancellationToken)
    {
        var result = await _accountService.TestConnectionAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<ClickUpMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetMembersAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/workspaces")]
    [ProducesResponseType(typeof(IReadOnlyList<ClickUpWorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkspaces(Guid id, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetAuthorizedWorkspacesAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("{id:guid}/tasks/query")]
    [ProducesResponseType(typeof(ClickUpTaskQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> QueryTasks(Guid id, [FromBody] ClickUpTaskQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetTasksAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
