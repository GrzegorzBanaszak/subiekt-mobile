using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SubiektMobile.Infrastructure.Persistence.Application;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260712120000_AddPalletLabelIssueAuditIndex")]
    partial class AddPalletLabelIssueAuditIndex
    {
    }
}
