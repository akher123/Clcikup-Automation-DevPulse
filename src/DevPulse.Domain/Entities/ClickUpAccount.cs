namespace DevPulse.Domain.Entities;

/// <summary>
/// Represents a connected ClickUp workspace with its own API token.
/// </summary>
public class ClickUpAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ClickUp workspace (team) identifier.
    /// </summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted personal access token or OAuth access token.
    /// </summary>
    public string EncryptedAccessToken { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastValidatedAtUtc { get; set; }

    public string? LastValidationMessage { get; set; }
}
