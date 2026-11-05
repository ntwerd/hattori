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
/// Create, read, update and soft-delete the todo items in the signed-in user's lists.
/// </summary>
[ApiController]
[Route("api/todo-items")]
[Authorize]
public class TodoItemsController : ControllerBase
{
    private readonly ITodoListRepository todoLists;
    private readonly ITodoItemRepository todoItems;

    public TodoItemsController(ITodoListRepository todoLists, ITodoItemRepository todoItems)
    {
        this.todoLists = todoLists;
        this.todoItems = todoItems;
    }

    /// <summary>
    /// Lists the live todo items of the signed-in user, all of them or one list's.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.TodoItem.READ)]
    [EndpointDescription("List the signed-in user's todo items, ordered by title. Filter to one list with ?todoListId=…")]
    public async Task<ActionResult<IReadOnlyList<TodoItemDto>>> GetItems([FromQuery] Guid? todoListId, CancellationToken cancellationToken)
    {
        Guid ownerId = getUserId();

        if (todoListId is not null && await todoLists.GetAsync(ownerId, todoListId.Value, cancellationToken) == null)
            return NotFound($"Todo list '{todoListId}' does not exist.");

        return Ok(await todoItems.ListAsync(ownerId, todoListId, cancellationToken));
    }

    /// <summary>
    /// Reads one live todo item of the signed-in user.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.TodoItem.READ)]
    [EndpointDescription("Get a single todo item by ID. Items of deleted lists and soft-deleted items answer 404.")]
    public async Task<ActionResult<TodoItemDto>> GetItem(Guid id, CancellationToken cancellationToken)
    {
        TodoItemDto? item = await todoItems.GetAsync(getUserId(), id, cancellationToken);

        if (item == null)
            return NotFound();

        return item;
    }

    /// <summary>
    /// Creates a todo item in one of the signed-in user's lists.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.TodoItem.WRITE)]
    [EndpointDescription("Create a todo item in one of the signed-in user's live lists. DueDate is a date with no time, like 2026-11-06.")]
    public async Task<ActionResult<TodoItemDto>> CreateItem(CreateTodoItemDto dto, CancellationToken cancellationToken)
    {
        Guid ownerId = getUserId();

        if (await todoLists.GetAsync(ownerId, dto.TodoListId, cancellationToken) == null)
            return NotFound($"Todo list '{dto.TodoListId}' does not exist.");

        TodoItemDto created = await todoItems.CreateAsync(ownerId, dto.TodoListId, dto.Title.Trim(), dto.Notes?.Trim(), dto.DueDate, cancellationToken);

        return CreatedAtAction(nameof(GetItem), new { id = created.Id }, created);
    }

    /// <summary>
    /// Changes one live todo item of the signed-in user, including its completion state.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.TodoItem.WRITE)]
    [EndpointDescription("Change a todo item. The whole item is replaced; completing stamps the completion time once, reopening clears it.")]
    public async Task<ActionResult<TodoItemDto>> UpdateItem(Guid id, UpdateTodoItemDto dto, CancellationToken cancellationToken)
    {
        TodoItemDto? updated = await todoItems.UpdateAsync(getUserId(), id, dto.Title.Trim(), dto.Notes?.Trim(), dto.IsCompleted, dto.DueDate, cancellationToken);

        if (updated == null)
            return NotFound();

        return updated;
    }

    /// <summary>
    /// Soft-deletes one live todo item of the signed-in user.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.TodoItem.WRITE)]
    [EndpointDescription("Soft-delete a todo item. The row stays, invisible to every read.")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        if (!await todoItems.SoftDeleteAsync(getUserId(), id, cancellationToken))
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
