using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Api.Security;

public static class SessionAuthentication
{
    public const string Scheme = "ApplicationSession";
    public const string CookieName = "subiekt_mobile_session";
    public const string ActorKindClaim = "actor_kind";
    public const string OrganizationIdClaim = "organization_id";
    public const string PermissionClaim = "permission";
    public const string SessionIdClaim = "session_id";
    public const string RequiresPasswordChangeClaim = "requires_password_change";
}

public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IIdentityAccessService _identityAccessService;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IIdentityAccessService identityAccessService)
        : base(options, logger, encoder)
    {
        _identityAccessService = identityAccessService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SessionAuthentication.CookieName, out var token))
        {
            return AuthenticateResult.NoResult();
        }

        var actor = await _identityAccessService.ResolveSessionAsync(token, Context.RequestAborted);
        if (actor is null)
        {
            return AuthenticateResult.Fail("Session is invalid, inactive or expired.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actor.Id.ToString()),
            new(ClaimTypes.Name, actor.DisplayName),
            new(SessionAuthentication.ActorKindClaim, actor.Kind.ToString()),
            new(SessionAuthentication.SessionIdClaim, actor.SessionId.ToString())
        };

        if (actor.RequiresPasswordChange)
        {
            claims.Add(new Claim(SessionAuthentication.RequiresPasswordChangeClaim, bool.TrueString));
        }

        if (actor.OrganizationId is Guid organizationId)
        {
            claims.Add(new Claim(SessionAuthentication.OrganizationIdClaim, organizationId.ToString()));
        }

        claims.AddRange(actor.Permissions.Select(permission =>
            new Claim(SessionAuthentication.PermissionClaim, permission)));

        var identity = new ClaimsIdentity(claims, SessionAuthentication.Scheme);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SessionAuthentication.Scheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}

public sealed class HttpCurrentActorAccessor : ICurrentActorAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentActorAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentActor? Actor
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true
                || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
                || !Guid.TryParse(principal.FindFirstValue(SessionAuthentication.SessionIdClaim), out var sessionId)
                || !Enum.TryParse<ActorKind>(
                    principal.FindFirstValue(SessionAuthentication.ActorKindClaim),
                    out var actorKind))
            {
                return null;
            }

            Guid? organizationId = Guid.TryParse(
                principal.FindFirstValue(SessionAuthentication.OrganizationIdClaim),
                out var parsedOrganizationId)
                ? parsedOrganizationId
                : null;

            return new CurrentActor(
                actorKind,
                actorId,
                organizationId,
                principal.Identity.Name ?? string.Empty,
                principal.FindAll(SessionAuthentication.PermissionClaim).Select(x => x.Value).ToList(),
                sessionId,
                principal.HasClaim(SessionAuthentication.RequiresPasswordChangeClaim, bool.TrueString));
        }
    }
}
