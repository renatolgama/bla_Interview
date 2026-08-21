using Bla.Domain.Enums;

namespace Bla.Domain.Entities;

// Named TaskItem (not Task) to avoid colliding with System.Threading.Tasks.Task.
public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
