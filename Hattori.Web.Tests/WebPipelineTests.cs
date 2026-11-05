// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hattori.Core.Configurations;
using Hattori.Core.DTOs;
using Hattori.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Hattori.Web.Tests;

/// <summary>
/// Whole-pipeline tests through the real app: routing, authentication, permission policies, cookies and the database.
/// </summary>
[TestFixture]
public class WebPipelineTests
{
    /// <summary>
    /// Boots the whole Hattori.Web app in Development, against the test database TestEnvironment points at.
    /// </summary>
    private class HattoriWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Migrations run here too, so this fixture also works when run alone.
        await using var setupContext = new HattoriDbContext(HattoriDatabaseRegistration.BuildOptions(DatabaseConnectionTarget.Test));
        await setupContext.Database.MigrateAsync();
    }

    [Test]
    public async Task UserCanRegisterCreateCompleteAndSoftDeleteATodo()
    {
        using var factory = new HattoriWebApplicationFactory();
        HttpClient client = factory.CreateClient();
        string email = $"user-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage register = await client.PostAsJsonAsync("/api/account/register", new { email, password = "Passw0rd!" });
        Assert.That(register.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), "registration signs the new account in and sets the cookie");

        HttpResponseMessage health = await client.GetAsync("/health");
        Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        HttpResponseMessage me = await client.GetAsync("/api/account/me");
        Assert.That(me.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        HttpResponseMessage createList = await client.PostAsJsonAsync("/api/todo-lists", new { name = "Chores", color = "#3A7BD5" });
        Assert.That(createList.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        TodoListDto? list = await createList.Content.ReadFromJsonAsync<TodoListDto>();
        Assert.That(list, Is.Not.Null);
        Assert.That(list!.Name, Is.EqualTo("chores"), "list names are stored in lowercase");

        HttpResponseMessage createItem = await client.PostAsJsonAsync("/api/todo-items", new { todoListId = list.Id, title = "Take out the trash", notes = (string?)null, dueDate = (DateOnly?)new DateOnly(2026, 11, 6) });
        Assert.That(createItem.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        TodoItemDto? item = await createItem.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.That(item, Is.Not.Null);

        HttpResponseMessage completeItem = await client.PutAsJsonAsync($"/api/todo-items/{item!.Id}", new { title = "Take out the trash", notes = (string?)null, isCompleted = true, dueDate = (DateOnly?)new DateOnly(2026, 11, 6) });
        Assert.That(completeItem.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        TodoItemDto? completed = await completeItem.Content.ReadFromJsonAsync<TodoItemDto>();

        Assert.Multiple(() =>
        {
            Assert.That(completed?.IsCompleted, Is.True);
            Assert.That(completed?.CompletedAt, Is.Not.Null);
        });

        HttpResponseMessage anonymous = await new HattoriWebApplicationFactory().CreateClient().GetAsync("/api/todo-lists");
        Assert.That(anonymous.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "the API answers 401, never a login redirect");

        HttpResponseMessage deleteList = await client.DeleteAsync($"/api/todo-lists/{list.Id}");
        Assert.That(deleteList.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        HttpResponseMessage readDeleted = await client.GetAsync($"/api/todo-lists/{list.Id}");
        Assert.That(readDeleted.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
