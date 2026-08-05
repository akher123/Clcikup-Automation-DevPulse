namespace DevPulse.Domain.Entities;

/// <summary>
/// Canonical developer record used for cross-workspace reporting.
/// </summary>
public class Developer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<DeveloperClickUpMapping> ClickUpMappings { get; set; } = [];
}
