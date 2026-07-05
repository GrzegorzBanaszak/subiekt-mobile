using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Identity;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.IdentityManage)]
[Route("api/admin")]
public sealed class AdministrationController : ControllerBase
{
    private readonly IIdentityAccessService _identityAccessService;

    public AdministrationController(IIdentityAccessService identityAccessService)
    {
        _identityAccessService = identityAccessService;
    }

    [HttpGet("administrators")]
    [Authorize(Policy = Permissions.AdministratorsManage)]
    public async Task<ActionResult<IReadOnlyList<AdministratorDto>>> GetAdministrators(CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.ListAdministratorsAsync(cancellationToken));

    [HttpPost("administrators")]
    [Authorize(Policy = Permissions.AdministratorsManage)]
    public async Task<ActionResult<AdministratorDto>> CreateAdministrator(
        CreateAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityAccessService.CreateAdministratorAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAdministrators), result);
    }

    [HttpPut("administrators/{id:guid}")]
    [Authorize(Policy = Permissions.AdministratorsManage)]
    public async Task<ActionResult<AdministratorDto>> UpdateAdministrator(
        Guid id,
        UpdateAdministratorRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.UpdateAdministratorAsync(id, request, cancellationToken));

    [HttpPost("administrators/{id:guid}/reset-password")]
    [Authorize(Policy = Permissions.AdministratorsManage)]
    public async Task<IActionResult> ResetAdministratorPassword(
        Guid id,
        ResetAdministratorPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _identityAccessService.ResetAdministratorPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("administrators/{id:guid}/active")]
    [Authorize(Policy = Permissions.AdministratorsManage)]
    public async Task<IActionResult> SetAdministratorActive(
        Guid id,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        await _identityAccessService.SetAdministratorActiveAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("organizations")]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> GetOrganizations(CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.ListOrganizationsAsync(cancellationToken));

    [HttpPost("organizations")]
    public async Task<ActionResult<OrganizationDto>> CreateOrganization(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityAccessService.CreateOrganizationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOrganizations), result);
    }

    [HttpPut("organizations/{id:guid}")]
    public async Task<ActionResult<OrganizationDto>> UpdateOrganization(
        Guid id,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.UpdateOrganizationAsync(id, request, cancellationToken));

    [HttpPut("organizations/{id:guid}/active")]
    public async Task<IActionResult> SetOrganizationActive(
        Guid id,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        await _identityAccessService.SetOrganizationActiveAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("organizations/{organizationId:guid}/employees")]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetEmployees(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.ListEmployeesAsync(organizationId, cancellationToken));

    [HttpPost("organizations/{organizationId:guid}/employees")]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee(
        Guid organizationId,
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityAccessService.CreateEmployeeAsync(organizationId, request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployees), new { organizationId }, result);
    }

    [HttpPut("organizations/{organizationId:guid}/employees/{employeeId:guid}")]
    public async Task<ActionResult<EmployeeDto>> UpdateEmployee(
        Guid organizationId,
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _identityAccessService.UpdateEmployeeAsync(organizationId, employeeId, request, cancellationToken));

    [HttpPut("organizations/{organizationId:guid}/employees/{employeeId:guid}/active")]
    public async Task<IActionResult> SetEmployeeActive(
        Guid organizationId,
        Guid employeeId,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        await _identityAccessService.SetEmployeeActiveAsync(
            organizationId,
            employeeId,
            request,
            cancellationToken);
        return NoContent();
    }
}
