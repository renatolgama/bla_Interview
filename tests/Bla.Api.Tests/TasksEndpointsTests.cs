using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bla.Application.Contracts.Auth;
using Bla.Application.Contracts.Common;
using Bla.Application.Contracts.Tasks;
using Bla.Domain.Enums;
using FluentAssertions;

namespace Bla.Api.Tests;

public class TasksEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TestWebApplicationFactory _factory = factory;

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "Test User", "Passw0rd1"));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static CreateTaskRequest NewTask(string title = "Write the report") =>
        new(title, "Quarterly numbers", new DateTime(2030, 1, 15, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithLocationAndTodoStatus()
    {
        var client = await CreateAuthenticatedClientAsync("create@example.com");

        var response = await client.PostAsJsonAsync("/api/tasks", NewTask());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        task!.Title.Should().Be("Write the report");
        task.Status.Should().Be(TaskItemStatus.Todo);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_Returns400ProblemDetails()
    {
        var client = await CreateAuthenticatedClientAsync("badtitle@example.com");

        var response = await client.PostAsJsonAsync("/api/tasks", NewTask(title: "   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task FullCrudFlow_CreateReadUpdateDelete()
    {
        var client = await CreateAuthenticatedClientAsync("crud@example.com");

        // Create
        var created = await (await client.PostAsJsonAsync("/api/tasks", NewTask()))
            .Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);

        // Read (single + list)
        var fetched = await client.GetFromJsonAsync<TaskResponse>(
            $"/api/tasks/{created!.Id}", JsonOptions);
        fetched!.Id.Should().Be(created.Id);
        var list = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>(
            "/api/tasks", JsonOptions);
        list!.Items.Should().ContainSingle(t => t.Id == created.Id);

        // Update
        var update = new UpdateTaskRequest(
            "Report sent", "Done and archived", TaskItemStatus.Done, created.DueDate);
        var updateResponse = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", update);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        updated!.Status.Should().Be(TaskItemStatus.Done);
        updated.UpdatedAt.Should().NotBeNull();

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/tasks/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterDelete = await client.GetAsync($"/api/tasks/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var client = await CreateAuthenticatedClientAsync("filter@example.com");
        var created = await (await client.PostAsJsonAsync("/api/tasks", NewTask("Open task")))
            .Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        var toFinish = await (await client.PostAsJsonAsync("/api/tasks", NewTask("Finished task")))
            .Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        await client.PutAsJsonAsync($"/api/tasks/{toFinish!.Id}",
            new UpdateTaskRequest("Finished task", null, TaskItemStatus.Done, null));

        var done = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>(
            "/api/tasks?status=Done", JsonOptions);

        done!.Items.Should().ContainSingle(t => t.Id == toFinish.Id);
        done.Items.Should().NotContain(t => t.Id == created!.Id);
    }

    [Fact]
    public async Task List_ReturnsPagingMetadata()
    {
        var client = await CreateAuthenticatedClientAsync("paging@example.com");
        for (var i = 1; i <= 3; i++)
        {
            await client.PostAsJsonAsync("/api/tasks", NewTask($"Task {i}"));
        }

        var page = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>(
            "/api/tasks?page=1&pageSize=2", JsonOptions);

        page!.Items.Should().HaveCount(2);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task List_WithInvalidPage_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync("badpage@example.com");

        var response = await client.GetAsync("/api/tasks?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_WithInvalidStatusValue_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync("badfilter@example.com");

        var response = await client.GetAsync("/api/tasks?status=Bogus");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_TaskOwnedByAnotherUser_Returns404()
    {
        var owner = await CreateAuthenticatedClientAsync("owner@example.com");
        var attacker = await CreateAuthenticatedClientAsync("attacker@example.com");
        var created = await (await owner.PostAsJsonAsync("/api/tasks", NewTask("Private task")))
            .Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);

        var response = await attacker.GetAsync($"/api/tasks/{created!.Id}");

        // 404, not 403: the API must not confirm the resource exists.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_TaskOwnedByAnotherUser_Returns404()
    {
        var owner = await CreateAuthenticatedClientAsync("owner2@example.com");
        var attacker = await CreateAuthenticatedClientAsync("attacker2@example.com");
        var created = await (await owner.PostAsJsonAsync("/api/tasks", NewTask("Private task")))
            .Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);

        var response = await attacker.PutAsJsonAsync($"/api/tasks/{created!.Id}",
            new UpdateTaskRequest("Hijacked", null, TaskItemStatus.Done, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
