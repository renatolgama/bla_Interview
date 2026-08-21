using Bla.Application.Abstractions;
using Bla.Application.Contracts.Tasks;
using Bla.Application.Exceptions;
using Bla.Application.Services;
using Bla.Application.Tests.Helpers;
using Bla.Domain.Entities;
using Bla.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Bla.Application.Tests;

public class TaskServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new TaskService(_taskRepository, _clock);
    }

    private static CreateTaskRequest ValidCreateRequest() =>
        new("  Write report  ", "Quarterly numbers", FixedNow.AddDays(1));

    private static UpdateTaskRequest ValidUpdateRequest() =>
        new("Updated title", "New description", TaskItemStatus.InProgress, FixedNow.AddDays(2));

    // ---------- Create ----------

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsTaskOwnedByUserWithTodoStatus()
    {
        var result = await _sut.CreateAsync(_userId, ValidCreateRequest(), default);

        result.Title.Should().Be("Write report"); // trimmed
        result.Status.Should().Be(TaskItemStatus.Todo);
        result.CreatedAt.Should().Be(FixedNow);
        result.UpdatedAt.Should().BeNull();
        await _taskRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(t =>
                t.UserId == _userId &&
                t.Title == "Write report" &&
                t.Status == TaskItemStatus.Todo &&
                t.Id != Guid.Empty),
            default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithMissingTitle_ThrowsValidation(string? title)
    {
        var request = new CreateTaskRequest(title!, null, null);

        var act = () => _sut.CreateAsync(_userId, request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("title");
    }

    [Fact]
    public async Task CreateAsync_WithTitleOver200Chars_ThrowsValidation()
    {
        var request = new CreateTaskRequest(new string('a', 201), null, null);

        var act = () => _sut.CreateAsync(_userId, request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("title");
    }

    [Fact]
    public async Task CreateAsync_WithDescriptionOver2000Chars_ThrowsValidation()
    {
        var request = new CreateTaskRequest("Valid", new string('d', 2001), null);

        var act = () => _sut.CreateAsync(_userId, request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("description");
    }

    [Fact]
    public async Task CreateAsync_WithPastDueDate_ThrowsValidation()
    {
        var request = new CreateTaskRequest("Valid", null, FixedNow.AddDays(-1));

        var act = () => _sut.CreateAsync(_userId, request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("dueDate");
    }

    [Fact]
    public async Task CreateAsync_WithDueDateToday_Succeeds()
    {
        // Midnight today is earlier than "now" but still the same day:
        // the rule compares dates, not instants.
        var request = new CreateTaskRequest("Valid", null, FixedNow.Date);

        var result = await _sut.CreateAsync(_userId, request, default);

        result.DueDate.Should().Be(FixedNow.Date);
    }

    [Fact]
    public async Task CreateAsync_WithoutDueDate_Succeeds()
    {
        var result = await _sut.CreateAsync(_userId, new CreateTaskRequest("Valid", null, null), default);

        result.DueDate.Should().BeNull();
    }

    // ---------- GetById ----------

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ReturnsIt()
    {
        var task = TaskBuilder.For(_userId).WithTitle("Mine").Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);

        var result = await _sut.GetByIdAsync(_userId, task.Id, default);

        result.Id.Should().Be(task.Id);
        result.Title.Should().Be("Mine");
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ThrowsNotFound()
    {
        var act = () => _sut.GetByIdAsync(_userId, Guid.NewGuid(), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskBelongsToAnotherUser_ThrowsNotFound()
    {
        var task = TaskBuilder.For(_otherUserId).Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);

        var act = () => _sut.GetByIdAsync(_userId, task.Id, default);

        // 404, not 403: the API must not reveal that the resource exists.
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesFieldsAndSetsUpdatedAt()
    {
        var task = TaskBuilder.For(_userId).Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);

        var result = await _sut.UpdateAsync(_userId, task.Id, ValidUpdateRequest(), default);

        result.Title.Should().Be("Updated title");
        result.Description.Should().Be("New description");
        result.Status.Should().Be(TaskItemStatus.InProgress);
        result.UpdatedAt.Should().Be(FixedNow);
        await _taskRepository.Received(1).UpdateAsync(task, default);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskBelongsToAnotherUser_ThrowsNotFound()
    {
        var task = TaskBuilder.For(_otherUserId).Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);

        var act = () => _sut.UpdateAsync(_userId, task.Id, ValidUpdateRequest(), default);

        await act.Should().ThrowAsync<NotFoundException>(); // 404, not 403
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskDoesNotExist_ThrowsNotFound()
    {
        var act = () => _sut.UpdateAsync(_userId, Guid.NewGuid(), ValidUpdateRequest(), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_WithMissingTitle_ThrowsValidation(string? title)
    {
        var task = TaskBuilder.For(_userId).Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);
        var request = new UpdateTaskRequest(title!, null, TaskItemStatus.Todo, null);

        var act = () => _sut.UpdateAsync(_userId, task.Id, request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("title");
    }

    [Fact]
    public async Task UpdateAsync_WithPastDueDate_Succeeds()
    {
        // The "no past due date" rule applies on creation only: editing an
        // already-overdue task must not force the user to change its date.
        var task = TaskBuilder.For(_userId).Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);
        var request = new UpdateTaskRequest("Valid", null, TaskItemStatus.Todo, FixedNow.AddDays(-5));

        var result = await _sut.UpdateAsync(_userId, task.Id, request, default);

        result.DueDate.Should().Be(FixedNow.AddDays(-5));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAsync_WhenOwnedByUser_DeletesTask()
    {
        var task = TaskBuilder.For(_userId).Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);

        await _sut.DeleteAsync(_userId, task.Id, default);

        await _taskRepository.Received(1).DeleteAsync(task, default);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskBelongsToAnotherUser_ThrowsNotFound()
    {
        var task = TaskBuilder.For(_otherUserId).Build();
        _taskRepository.GetByIdAsync(task.Id, default).Returns(task);

        var act = () => _sut.DeleteAsync(_userId, task.Id, default);

        await act.Should().ThrowAsync<NotFoundException>();
        await _taskRepository.DidNotReceive().DeleteAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    // ---------- GetAll ----------

    [Fact]
    public async Task GetAllAsync_ReturnsMappedTasksWithPagingMetadata()
    {
        var tasks = new List<TaskItem>
        {
            TaskBuilder.For(_userId).WithTitle("First").Build(),
            TaskBuilder.For(_userId).WithTitle("Second").WithStatus(TaskItemStatus.Done).Build()
        };
        _taskRepository.GetByUserAsync(_userId, null, 1, 10, default)
            .Returns(new PagedResult<TaskItem>(tasks, 2));

        var result = await _sut.GetAllAsync(_userId, null, 1, 10, default);

        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("First");
        result.Items[1].Status.Should().Be(TaskItemStatus.Done);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ComputesTotalPagesFromTotalCount()
    {
        _taskRepository.GetByUserAsync(_userId, null, 2, 10, default)
            .Returns(new PagedResult<TaskItem>([TaskBuilder.For(_userId).Build()], 25));

        var result = await _sut.GetAllAsync(_userId, null, 2, 10, default);

        result.TotalPages.Should().Be(3); // ceil(25 / 10)
    }

    [Fact]
    public async Task GetAllAsync_ForwardsStatusFilterAndPagingToRepository()
    {
        _taskRepository.GetByUserAsync(_userId, TaskItemStatus.Done, 3, 20, default)
            .Returns(new PagedResult<TaskItem>([], 0));

        await _sut.GetAllAsync(_userId, TaskItemStatus.Done, 3, 20, default);

        await _taskRepository.Received(1)
            .GetByUserAsync(_userId, TaskItemStatus.Done, 3, 20, default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAllAsync_WithPageBelowOne_ThrowsValidation(int page)
    {
        var act = () => _sut.GetAllAsync(_userId, null, page, 10, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task GetAllAsync_WithPageSizeOutOfRange_ThrowsValidation(int pageSize)
    {
        var act = () => _sut.GetAllAsync(_userId, null, 1, pageSize, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("pageSize");
    }
}
