namespace DevPulse.Infrastructure.ClickUp.Models;

internal sealed class ClickUpTeamsResponse
{
    public List<ClickUpTeam> Teams { get; set; } = [];
}

internal sealed class ClickUpTeam
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

internal sealed class ClickUpMembersResponse
{
    public List<ClickUpMemberItem> Members { get; set; } = [];
}

internal sealed class ClickUpMemberItem
{
    public ClickUpUser User { get; set; } = new();
}

internal sealed class ClickUpUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ProfilePicture { get; set; }
}

internal sealed class ClickUpTasksResponse
{
    public List<ClickUpTaskItem> Tasks { get; set; } = [];
}

internal sealed class ClickUpTaskItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public long? DateCreated { get; set; }
    public long? DateClosed { get; set; }
    public long? DueDate { get; set; }
    public ClickUpStatus? Status { get; set; }
    public List<ClickUpUser>? Assignees { get; set; }
    public ClickUpList? List { get; set; }
}

internal sealed class ClickUpStatus
{
    public string Status { get; set; } = string.Empty;
}

internal sealed class ClickUpList
{
    public string Name { get; set; } = string.Empty;
}
