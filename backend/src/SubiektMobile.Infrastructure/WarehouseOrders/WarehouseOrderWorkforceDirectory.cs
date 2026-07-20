using Microsoft.EntityFrameworkCore;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Domain.WarehouseOrders;
using SubiektMobile.Infrastructure.Persistence.Application;

namespace SubiektMobile.Infrastructure.WarehouseOrders;

public sealed class WarehouseOrderWorkforceDirectory(ApplicationDbContext dbContext) : IWarehouseOrderWorkforceDirectory
{
    public async Task<IReadOnlyList<AvailableWarehouseOrderAssigneeDto>> ListAvailableAsync(CancellationToken ct) =>
        await (from employee in dbContext.Employees.AsNoTracking()
               join organization in dbContext.Organizations.AsNoTracking()
                   on employee.OrganizationId equals organization.Id
               where employee.IsActive && organization.IsActive
               orderby organization.Name, employee.DisplayName
               select new AvailableWarehouseOrderAssigneeDto(employee.Id, organization.Id,
                   employee.DisplayName, organization.Name)).ToListAsync(ct);

    public async Task<IReadOnlyList<WarehouseOrderAssigneeCandidate>> ResolveActiveAsync(
        IReadOnlyCollection<Guid> employeeIds, CancellationToken ct)
    {
        var ids = employeeIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        return await (from employee in dbContext.Employees.AsNoTracking()
                      join organization in dbContext.Organizations.AsNoTracking()
                          on employee.OrganizationId equals organization.Id
                      where ids.Contains(employee.Id) && employee.IsActive && organization.IsActive
                      select new WarehouseOrderAssigneeCandidate(employee.Id, organization.Id, employee.DisplayName))
            .ToListAsync(ct);
    }
}
