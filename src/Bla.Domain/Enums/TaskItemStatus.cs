namespace Bla.Domain.Enums;

// Named TaskItemStatus (not TaskStatus) to avoid colliding with
// System.Threading.Tasks.TaskStatus in async code.
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}
