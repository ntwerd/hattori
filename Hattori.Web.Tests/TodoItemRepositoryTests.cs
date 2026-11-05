// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hattori.Core.Configurations;
using Hattori.Core.DTOs;
using Hattori.Core.Entities;
using Hattori.Persistence;
using Hattori.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Hattori.Web.Tests;

/// <summary>
/// TodoItemRepository tests against the real PostgreSQL test database.
/// </summary>
[TestFixture]
public class TodoItemRepositoryTests
{
    private DbContextOptions<HattoriDbContext> dbOptions = null!;
    private HattoriDbContext context = null!;
    private TodoItemRepository repository = null!;
    private Guid ownerId = Guid.NewGuid();
    private Guid listId = Guid.NewGuid();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        dbOptions = HattoriDatabaseRegistration.BuildOptions(DatabaseConnectionTarget.Test);

        await using var setupContext = new HattoriDbContext(dbOptions);
        await setupContext.Database.MigrateAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        context = new HattoriDbContext(dbOptions);
        repository = new TodoItemRepository(context);
        ownerId = Guid.NewGuid();

        var list = new TodoList
        {
            OwnerId = ownerId,
            Name = $"list-{ownerId:N}"
        };

        context.Set<TodoList>().Add(list);
        await context.SaveChangesAsync();
        listId = list.Id;
    }

    [TearDown]
    public async Task TearDown()
    {
        await context.Set<TodoItem>().IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Set<TodoList>().IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.DisposeAsync();
    }

    [Test]
    public async Task CreatedItemCanBeRead()
    {
        TodoItemDto created = await repository.CreateAsync(ownerId, listId, "Take out the trash", "Both bins", new DateOnly(2026, 11, 6), CancellationToken.None);

        TodoItemDto? read = await repository.GetAsync(ownerId, created.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(read?.Title, Is.EqualTo("Take out the trash"));
            Assert.That(read?.Notes, Is.EqualTo("Both bins"));
            Assert.That(read?.DueDate, Is.EqualTo(new DateOnly(2026, 11, 6)));
            Assert.That(read?.UpdatedAt, Is.Not.Null, "the audit interceptor stamps every save");
        });
    }

    [Test]
    public async Task ListAsyncFiltersToOneListsItems()
    {
        await repository.CreateAsync(ownerId, listId, "Keep", null, null, CancellationToken.None);
        await repository.CreateAsync(ownerId, listId, "Also keep", null, null, CancellationToken.None);

        var otherList = new TodoList
        {
            OwnerId = ownerId,
            Name = $"other-{ownerId:N}"
        };

        context.Set<TodoList>().Add(otherList);
        await context.SaveChangesAsync();
        await repository.CreateAsync(ownerId, otherList.Id, "Filtered out", null, null, CancellationToken.None);

        var items = await repository.ListAsync(ownerId, listId, CancellationToken.None);

        Assert.That(items.Select(item => item.Title), Is.EqualTo(new[] { "Also keep", "Keep" }));
    }

    [Test]
    public async Task ItemInAnotherUsersListIsInvisible()
    {
        TodoItemDto created = await repository.CreateAsync(ownerId, listId, "Secret", null, null, CancellationToken.None);

        TodoItemDto? read = await repository.GetAsync(Guid.NewGuid(), created.Id, CancellationToken.None);

        Assert.That(read, Is.Null);
    }

    [Test]
    public async Task CompletingStampsCompletedAtAndReopeningClearsIt()
    {
        TodoItemDto created = await repository.CreateAsync(ownerId, listId, "Water the plants", null, null, CancellationToken.None);

        TodoItemDto? completed = await repository.UpdateAsync(ownerId, created.Id, "Water the plants", null, true, null, CancellationToken.None);
        TodoItemDto? reopened = await repository.UpdateAsync(ownerId, created.Id, "Water the plants", null, false, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(completed?.IsCompleted, Is.True);
            Assert.That(completed?.CompletedAt, Is.Not.Null);
            Assert.That(reopened?.IsCompleted, Is.False);
            Assert.That(reopened?.CompletedAt, Is.Null);
        });
    }

    [Test]
    public async Task SoftDeletedItemIsInvisible()
    {
        TodoItemDto created = await repository.CreateAsync(ownerId, listId, "Hide me", null, null, CancellationToken.None);

        bool deleted = await repository.SoftDeleteAsync(ownerId, created.Id, CancellationToken.None);
        TodoItemDto? read = await repository.GetAsync(ownerId, created.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(read, Is.Null);
        });
    }
}
