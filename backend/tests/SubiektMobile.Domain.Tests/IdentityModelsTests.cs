using SubiektMobile.Domain.Identity;
using Xunit;

namespace SubiektMobile.Domain.Tests;

public sealed class IdentityModelsTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Administrator_normalizes_login_and_can_be_deactivated()
    {
        var administrator = Administrator.Create(" Admin.Test ", "Administrator", "hash", false, Now);

        administrator.SetActive(false, Now.AddMinutes(1));

        Assert.Equal("Admin.Test", administrator.Username);
        Assert.Equal("ADMIN.TEST", administrator.NormalizedUsername);
        Assert.False(administrator.IsActive);
        Assert.Equal(Now.AddMinutes(1), administrator.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("has space")]
    public void Administrator_rejects_invalid_login(string username)
    {
        Assert.Throws<ArgumentException>(() =>
            Administrator.Create(username, "Administrator", "hash", false, Now));
    }

    [Fact]
    public void Changing_temporary_password_clears_requirement()
    {
        var administrator = Administrator.Create(
            "admin",
            "Administrator",
            "temporary-hash",
            false,
            Now,
            requiresPasswordChange: true);

        administrator.ChangePassword("new-hash", Now.AddMinutes(1));

        Assert.False(administrator.RequiresPasswordChange);
        Assert.Equal("new-hash", administrator.PasswordHash);
        Assert.Equal(Now.AddMinutes(1), administrator.UpdatedAtUtc);
    }

    [Fact]
    public void Employee_requires_organization_and_normalizes_code()
    {
        var organizationId = Guid.NewGuid();
        var employee = Employee.Create(organizationId, " mg-01 ", "Jan Kowalski", Now);

        Assert.Equal(organizationId, employee.OrganizationId);
        Assert.Equal("mg-01", employee.Code);
        Assert.Equal("MG-01", employee.NormalizedCode);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public void Password_policy_rejects_passwords_shorter_than_twelve_characters(string password)
    {
        Assert.Throws<ArgumentException>(() => IdentityRules.ValidatePassword(password));
    }

    [Fact]
    public void Password_policy_accepts_twelve_characters()
    {
        IdentityRules.ValidatePassword("123456789012");
    }
}
