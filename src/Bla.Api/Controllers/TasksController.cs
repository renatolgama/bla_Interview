using Bla.Api.Extensions;
using Bla.Application.Contracts.Common;
using Bla.Application.Contracts.Tasks;
using Bla.Application.Services;
using Bla.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bla.Api.Controllers;

// Every action is scoped to the authenticated user taken from the JWT:
// controllers never accept a user id from the request.
[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<TaskResponse>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] TaskItemStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10) =>
        Ok(await taskService.GetAllAsync(User.GetUserId(), status, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        Ok(await taskService.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(
        CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var response = await taskService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid id, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        Ok(await taskService.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
