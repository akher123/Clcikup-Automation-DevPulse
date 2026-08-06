namespace DevPulse.Domain.Entities;

    /// <summary>
    /// Canonical developer record used for cross-workspace reporting.
    /// <see cref="IsActive"/> is a registry flag only; inactive developers remain included in analytics but are excluded from report filters.
    /// </summary>
    public class Developer
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        /// <summary>
        /// Registry status for the Developers page. Does not exclude the developer from reports or analytics.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<DeveloperClickUpMapping> ClickUpMappings { get; set; } = [];
    }
