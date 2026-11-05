// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Hattori.Core.Authorization;
using Hattori.Core.DTOs;
using Hattori.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hattori.Web.Controllers;

/// <summary>
/// Register an account and read one's own account. The identity API's own endpoints under /account cover login, refresh and logout.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<HattoriUser> userManager;
    private readonly RoleManager<IdentityRole<Guid>> roleManager;
    private readonly SignInManager<HattoriUser> signInManager;

    public AccountController(UserManager<HattoriUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, SignInManager<HattoriUser> signInManager)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.signInManager = signInManager;
    }

    /// <summary>
    /// Creates an account with the default user role and signs it in. This is the public registration; /account/register is disabled.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [EndpointDescription("Create an account and sign in. The account receives the default user role. Passwords need an uppercase letter, a lowercase letter, a digit and a symbol.")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new HattoriUser
        {
            UserName = dto.Email,
            Email = dto.Email
        };

        IdentityResult created = await userManager.CreateAsync(user, dto.Password);

        if (!created.Succeeded)
            return ValidationProblem(new ValidationProblemDetails(created.Errors.ToDictionary(error => error.Code, error => new[] { error.Description })));

        await ensureDefaultRoleAsync();
        await userManager.AddToRoleAsync(user, DefaultRoles.USER);
        await signInManager.SignInAsync(user, isPersistent: false);

        return NoContent();
    }

    /// <summary>
    /// Reads the signed-in user's own account.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = Permissions.Account.READ)]
    [EndpointDescription("Get the signed-in user's ID and email address.")]
    public async Task<ActionResult<AccountDto>> GetMe()
    {
        HattoriUser? user = await userManager.GetUserAsync(User);

        if (user == null)
            return NotFound();

        return new AccountDto(user.Id, user.Email!);
    }

    /// <summary>
    /// Creates the default user role with its permission claims if it does not exist yet. Idempotent, so the first registration can call it.
    /// </summary>
    private async Task ensureDefaultRoleAsync()
    {
        if (await roleManager.RoleExistsAsync(DefaultRoles.USER))
            return;

        var role = new IdentityRole<Guid>(DefaultRoles.USER);
        await roleManager.CreateAsync(role);

        foreach (string permission in DefaultRoles.PermissionsFor(DefaultRoles.USER))
            await roleManager.AddClaimAsync(role, new Claim(Permissions.CLAIM_TYPE, permission));
    }
}
