using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Gateway.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IUserService users) : ControllerBase
{
    /// <summary>One of only two endpoints in the whole system that do not require a bearer token -
    /// LoginBranding below is the other.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<DtoLoginResult>> Login([FromBody] LoginRequestDto request, CancellationToken ct) =>
        Ok(await users.LoginAsync(request, ct));

    /// <summary>The progressive/identifier-first login screen's pre-auth lookup - see
    /// GetLoginBrandingAsync's own doc comment for the non-enumeration discipline this depends
    /// on. Always 200 with an empty DtoClientBranding rather than 404/204, precisely so a
    /// network trace can never distinguish "no branding" from "unknown username" from
    /// "known username, no Client".</summary>
    [HttpGet("login/branding")]
    [AllowAnonymous]
    public async Task<ActionResult<DtoClientBranding>> LoginBranding([FromQuery] string username, CancellationToken ct) =>
        Ok(await users.GetLoginBrandingAsync(username, ct));

    [HttpGet]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<List<DtoUserSummary>>> List([FromQuery] Guid? clientId, CancellationToken ct) =>
        Ok(await users.ListUsersAsync(this.GetUserCallerScope(), clientId, ct));

    [HttpPost]
    [Authorize(Roles = "Admin,BaseRole")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<DtoUserSummary>> Create([FromBody] CreateUserDto request, CancellationToken ct)
    {
        var user = await users.CreateUserAsync(request, this.GetUserCallerScope(), ct);
        return CreatedAtAction(nameof(List), user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<DtoUserSummary>> Update(Guid id, [FromBody] UpdateUserDto request, CancellationToken ct)
    {
        var user = await users.UpdateUserAsync(id, request, this.GetUserCallerScope(), ct);
        return user is null ? NotFound() : Ok(user);
    }

    // Self-service - CallerId always comes from the caller's own validated JWT, never from a
    // client-supplied id, so there's no way to reach another user's profile through these.
    [HttpGet("me")]
    public async Task<ActionResult<DtoUserSummary>> Me(CancellationToken ct)
    {
        var user = await users.GetUserAsync(CallerId, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<DtoUserSummary>> UpdateMe([FromBody] UpdateMyProfileDto request, CancellationToken ct)
    {
        var user = await users.UpdateMyProfileAsync(CallerId, request, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("me/change-password")]
    public async Task<ActionResult<DtoUserSummary>> ChangeMyPassword([FromBody] ChangePasswordDto request, CancellationToken ct)
    {
        var user = await users.ChangePasswordAsync(CallerId, request, ct);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Self-service, for white-labeling - no role restriction, any authenticated caller.
    /// 204 (not a body-less 200) when they belong to no Client, so the Blazor client's
    /// ClientBrandingProvider can tell "definitely no branding" apart from a transient failure.</summary>
    [HttpGet("me/branding")]
    public async Task<ActionResult<DtoClientBranding>> MyBranding(CancellationToken ct)
    {
        var branding = await users.GetMyBrandingAsync(this.GetUserCallerScope(), ct);
        return branding is null ? NoContent() : Ok(branding);
    }

    /// <summary>The "App Theme" button - Admin-only (Technician/Support/BaseRole never see it in
    /// the UI, and this rejects them here too), and even then only ever touches the caller's own
    /// Client - see Client.API's UpdateClientBranding for the independent re-check that depends
    /// on.</summary>
    [HttpPut("me/branding")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DtoClientBranding>> UpdateMyBranding([FromBody] UpdateMyBrandingDto request, CancellationToken ct)
    {
        var branding = await users.UpdateMyBrandingAsync(this.GetUserCallerScope(), request, ct);
        return branding is null ? NotFound() : Ok(branding);
    }

    // .NET's JwtBearer handler maps the "sub" claim to ClaimTypes.NameIdentifier by default,
    // but that mapping is version/config-dependent - checking the raw "sub" too makes this
    // robust either way rather than betting on which behavior is currently in effect.
    private Guid CallerId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
