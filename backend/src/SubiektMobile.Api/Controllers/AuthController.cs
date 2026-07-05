using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SubiektMobile.Api.Security;
using SubiektMobile.Application.Identity;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IIdentityAccessService _identityAccessService;
    private readonly ICurrentActorAccessor _currentActorAccessor;
    private readonly IAntiforgery _antiforgery;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IIdentityAccessService identityAccessService,
        ICurrentActorAccessor currentActorAccessor,
        IAntiforgery antiforgery,
        IWebHostEnvironment environment)
    {
        _identityAccessService = identityAccessService;
        _currentActorAccessor = currentActorAccessor;
        _antiforgery = antiforgery;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet("csrf-token")]
    public ActionResult<CsrfTokenResponse> GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new CsrfTokenResponse(tokens.RequestToken ?? string.Empty, "X-CSRF-TOKEN"));
    }

    [AllowAnonymous]
    [EnableRateLimiting("identity-public")]
    [HttpPost("bootstrap-administrator")]
    public async Task<ActionResult<CurrentSessionResponse>> BootstrapAdministrator(
        [FromHeader(Name = "X-Setup-Token")] string setupToken,
        BootstrapAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _identityAccessService.BootstrapAdministratorAsync(
            request,
            setupToken,
            CurrentToken(),
            cancellationToken);
        SetSessionCookie(session);
        return StatusCode(StatusCodes.Status201Created, Map(session));
    }

    [AllowAnonymous]
    [EnableRateLimiting("identity-public")]
    [HttpPost("administrator/sign-in")]
    public async Task<ActionResult<CurrentSessionResponse>> SignInAdministrator(
        AdministratorSignInRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _identityAccessService.SignInAdministratorAsync(
            request,
            CurrentToken(),
            cancellationToken);
        SetSessionCookie(session);
        return Ok(Map(session));
    }

    [AllowAnonymous]
    [HttpGet("organizations")]
    public async Task<ActionResult<IReadOnlyList<PublicOrganizationDto>>> GetOrganizations(CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.ListPublicOrganizationsAsync(cancellationToken));

    [AllowAnonymous]
    [HttpGet("organizations/{organizationId:guid}/employees")]
    public async Task<ActionResult<IReadOnlyList<PublicEmployeeDto>>> GetEmployees(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.ListPublicEmployeesAsync(organizationId, cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("identity-public")]
    [HttpPost("employee/select")]
    public async Task<ActionResult<CurrentSessionResponse>> SelectEmployee(
        SelectEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _identityAccessService.SelectEmployeeAsync(
            request,
            CurrentToken(),
            cancellationToken);
        SetSessionCookie(session);
        return Ok(Map(session));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentActor> GetCurrentActor() => Ok(_currentActorAccessor.Actor!);

    [Authorize]
    [HttpPost("administrator/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeAdministratorPassword(
        ChangeOwnPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _identityAccessService.ChangeOwnPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        var token = CurrentToken();
        if (token is not null)
        {
            await _identityAccessService.SignOutAsync(token, cancellationToken);
        }

        Response.Cookies.Delete(SessionAuthentication.CookieName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            Path = "/"
        });
        return NoContent();
    }

    private string? CurrentToken() =>
        Request.Cookies.TryGetValue(SessionAuthentication.CookieName, out var token) ? token : null;

    private void SetSessionCookie(SessionIssued session)
    {
        Response.Cookies.Append(SessionAuthentication.CookieName, session.Token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            IsEssential = true,
            Expires = session.ExpiresAtUtc,
            Path = "/"
        });
    }

    private static CurrentSessionResponse Map(SessionIssued session) =>
        new(session.ExpiresAtUtc, session.Actor);
}

public sealed record CsrfTokenResponse(string Token, string HeaderName);
public sealed record CurrentSessionResponse(DateTimeOffset ExpiresAtUtc, CurrentActor Actor);
