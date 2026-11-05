// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;
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
/// TodoListRepository tests against the real PostgreSQL test database.
/// </summary>
[TestFixture]
public class TodoListRepositoryTests
{
    private DbContextOptions<HattoriDbContext> dbOptions = null!;
    private HattoriDbContext context = null!;
    private TodoListRepository repository = null!;
    private Guid ownerId = Guid.NewGuid();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        dbOptions = HattoriDatabaseRegistration.BuildOptions(DatabaseConnectionTarget.Test);

        await using var setupContext = new HattoriDbContext(dbOptions);
        await setupContext.Database.MigrateAsync();
    }

    [SetUp]
    public void SetUp()
    {
        context = new HattoriDbContext(dbOptions);
        repository = new TodoListRepository(context);
        ownerId = Guid.NewGuid();
    }

    [TearDown]
    public async Task TearDown()
    {
        await context.Set<TodoItem>().IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Set<TodoList>().IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.DisposeAsync();
    }

    [Test]
    public async Task CreatedListCanBeRead()
    {
        TodoListDto created = await repository.CreateAsync(ownerId, "urgent", "#3A7BD5", CancellationToken.None);

        TodoListDto? read = await repository.GetAsync(ownerId, created.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(read?.Name, Is.EqualTo("urgent"));
            Assert.That(read?.Color, Is.EqualTo("#3A7BD5"));
            Assert.That(read?.UpdatedAt, Is.Not.Null, "the audit interceptor stamps every save");
        });
    }

    [Test]
    public async Task ListAsyncReturnsOnlyTheOwnersLists()
    {
        Guid otherOwner = Guid.NewGuid();
        await repository.CreateAsync(ownerId, "mine", null, CancellationToken.None);
        await repository.CreateAsync(otherOwner, "theirs", null, CancellationToken.None);

        IReadOnlyList<TodoListDto> lists = await repository.ListAsync(ownerId, CancellationToken.None);

        Assert.That(lists.Select(list => list.Name), Is.EqualTo(new[] { "mine" }));
    }

    [Test]
    public async Task AnotherUsersListIsInvisible()
    {
        TodoListDto created = await repository.CreateAsync(ownerId, "secret", null, CancellationToken.None);

        TodoListDto? read = await repository.GetAsync(Guid.NewGuid(), created.Id, CancellationToken.None);

        Assert.That(read, Is.Null);
    }

    [Test]
    public async Task DuplicateNameIsDetected()
    {
        await repository.CreateAsync(ownerId, "chores", null, CancellationToken.None);

        bool exists = await repository.ExistsAsync(ownerId, "chores", CancellationToken.None);

        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task UpdateChangesNameAndColor()
    {
        TodoListDto created = await repository.CreateAsync(ownerId, "chores", "#3A7BD5", CancellationToken.None);

        TodoListDto? updated = await repository.UpdateAsync(ownerId, created.Id, "groceries", "#FF0000", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated?.Name, Is.EqualTo("groceries"));
            Assert.That(updated?.Color, Is.EqualTo("#FF0000"));
        });
    }

    [Test]
    public async Task SoftDeletedListIsInvisibleAndFreesItsName()
    {
        TodoListDto created = await repository.CreateAsync(ownerId, "chores", null, CancellationToken.None);

        bool deleted = await repository.SoftDeleteAsync(ownerId, created.Id, CancellationToken.None);

        Assert.That(deleted, Is.True);
        Assert.That(await repository.GetAsync(ownerId, created.Id, CancellationToken.None), Is.Null);
        Assert.That(await repository.ExistsAsync(ownerId, "chores", CancellationToken.None), Is.False);
    }
}
