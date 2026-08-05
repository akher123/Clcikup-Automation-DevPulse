using DevPulse.Application.Abstractions.Developers;
using DevPulse.Shared.Contracts.Developers;
using Microsoft.AspNetCore.Mvc;

namespace DevPulse.Server.Controllers;

[ApiController]
[Route("api/developers")]
public sealed class DevelopersController : ControllerBase
{
    private readonly IDeveloperService _developerService;

    public DevelopersController(IDeveloperService developerService)
    {
        _developerService = developerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeveloperDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var developers = await _developerService.GetAllAsync(cancellationToken);
        return Ok(developers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeveloperDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _developerService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [ProducesResponseType(typeof(DeveloperDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDeveloperRequest request, CancellationToken cancellationToken)
    {
        var result = await _developerService.CreateAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error, errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DeveloperDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeveloperRequest request, CancellationToken cancellationToken)
    {
        var result = await _developerService.UpdateAsync(id, request, cancellationToken);
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
        var result = await _developerService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpPost("{id:guid}/mappings")]
    [ProducesResponseType(typeof(DeveloperDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMapping(Guid id, [FromBody] AddDeveloperMappingRequest request, CancellationToken cancellationToken)
    {
        var result = await _developerService.AddMappingAsync(id, request, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error, errors = result.Errors });
        }

        return Ok(result.Value);
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(SyncDevelopersResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SyncFromClickUp(CancellationToken cancellationToken)
    {
        var result = await _developerService.SyncFromClickUpAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
