// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Hattori.Core.Authorization;
using Hattori.Core.DTOs;
using Hattori.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hattori.Web.Controllers;

/// <summary>
/// Create, read, update and soft-delete the signed-in user's todo lists.
/// </summary>
[ApiController]
[Route("api/todo-lists")]
[Authorize]
public class TodoListsController : ControllerBase
{
    private readonly ITodoListRepository todoLists;

    public TodoListsController(ITodoListRepository todoLists)
    {
        this.todoLists = todoLists;
    }

    /// <summary>
    /// Lists every live todo list of the signed-in user.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.TodoList.READ)]
    [EndpointDescription("List the signed-in user's todo lists, ordered by name.")]
    public async Task<ActionResult<IReadOnlyList<TodoListDto>>> GetLists(CancellationToken cancellationToken)
    {
        return Ok(await todoLists.ListAsync(getUserId(), cancellationToken));
    }

    /// <summary>
    /// Reads one live todo list of the signed-in user.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.TodoList.READ)]
    [EndpointDescription("Get a single todo list by ID. Soft-deleted lists answer 404.")]
    public async Task<ActionResult<TodoListDto>> GetList(Guid id, CancellationToken cancellationToken)
    {
        TodoListDto? list = await todoLists.GetAsync(getUserId(), id, cancellationToken);

        if (list == null)
            return NotFound();

        return list;
    }

    /// <summary>
    /// Creates a todo list for the signed-in user.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.TodoList.WRITE)]
    [EndpointDescription("Create a todo list. Names are unique per user, case-insensitive, stored in lowercase; colors are CSS hex like #3A7BD5.")]
    public async Task<ActionResult<TodoListDto>> CreateList(CreateTodoListDto dto, CancellationToken cancellationToken)
    {
        Guid ownerId = getUserId();
        string name = dto.Name.Trim().ToLowerInvariant();

        if (await todoLists.ExistsAsync(ownerId, name, cancellationToken))
            return Conflict($"A todo list named '{name}' already exists.");

        TodoListDto created = await todoLists.CreateAsync(ownerId, name, dto.Color?.Trim(), cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = created.Id }, created);
    }

    /// <summary>
    /// Renames or recolors one live todo list of the signed-in user.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.TodoList.WRITE)]
    [EndpointDescription("Rename or recolor a todo list. Same name rules as creation.")]
    public async Task<ActionResult<TodoListDto>> UpdateList(Guid id, UpdateTodoListDto dto, CancellationToken cancellationToken)
    {
        Guid ownerId = getUserId();
        string name = dto.Name.Trim().ToLowerInvariant();
        TodoListDto? existing = await todoLists.GetAsync(ownerId, id, cancellationToken);

        if (existing == null)
            return NotFound();

        if (name != existing.Name && await todoLists.ExistsAsync(ownerId, name, cancellationToken))
            return Conflict($"A todo list named '{name}' already exists.");

        TodoListDto? updated = await todoLists.UpdateAsync(ownerId, id, name, dto.Color?.Trim(), cancellationToken);

        if (updated == null)
            return NotFound();

        return updated;
    }

    /// <summary>
    /// Soft-deletes one live todo list of the signed-in user. Its items become invisible with it.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.TodoList.WRITE)]
    [EndpointDescription("Soft-delete a todo list. The row stays and its name can be reused; its items become invisible.")]
    public async Task<IActionResult> DeleteList(Guid id, CancellationToken cancellationToken)
    {
        if (!await todoLists.SoftDeleteAsync(getUserId(), id, cancellationToken))
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Reads the signed-in user's ID from the name identifier claim every authenticated request carries.
    /// </summary>
    /// <returns>The user's ID.</returns>
    /// <exception cref="InvalidOperationException">The claim is missing or malformed, which an authenticated request should prevent.</exception>
    private Guid getUserId()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userId, out Guid parsed) ? parsed : throw new InvalidOperationException("The signed-in user has no valid ID claim.");
    }
}
