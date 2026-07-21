using SubiektMobile.Domain.Customers;
using Xunit;

namespace SubiektMobile.Domain.Tests;

public sealed class CustomerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Customer_normalizes_code_and_increments_version_when_a_site_is_added()
    {
        var customer = Customer.Create(Guid.NewGuid(), " krm-001 ", "Kramp", null, null, null, true, Now);

        var site = customer.AddSite(Guid.NewGuid(), " leipzig ", "Leipzig", "de", true, Now.AddMinutes(1));

        Assert.Equal("krm-001", customer.Code);
        Assert.Equal("KRM-001", customer.NormalizedCode);
        Assert.Equal("DE", site.CountryCode);
        Assert.Equal(2, customer.Version);
    }

    [Fact]
    public void Logistics_profile_is_complete_only_after_required_vda_data_is_provided()
    {
        var customer = Customer.Create(Guid.NewGuid(), "KRM", "Kramp", null, null, null, true, Now);
        var site = customer.AddSite(Guid.NewGuid(), "LEI", "Leipzig", "DE", true, Now);

        site.ConfigureLogisticsProfile(new CustomerSiteProfileInput(
            "Kramp GmbH", "Siemensstraße 1", "04420", "Markranstädt", "Tor 12", null, "88942-DE",
            "EPAL 1200×800", 1050, true, true, false, null, VdaLabelProfile.Vda4902), Now.AddMinutes(1));

        Assert.NotNull(site.LogisticsProfile);
        Assert.True(site.LogisticsProfile!.IsComplete);
        Assert.Equal(2, site.Version);
    }

    [Fact]
    public void Draft_logistics_profile_accepts_missing_operational_data_but_rejects_invalid_height()
    {
        var customer = Customer.Create(Guid.NewGuid(), "KRM", "Kramp", null, null, null, true, Now);
        var site = customer.AddSite(Guid.NewGuid(), "WAW", "Warszawa", "PL", true, Now);

        site.ConfigureLogisticsProfile(new CustomerSiteProfileInput(
            null, null, null, null, null, null, null, null, null, false, false, false, null, null),
            Now.AddMinutes(1));

        Assert.False(site.LogisticsProfile!.IsComplete);
        Assert.Throws<ArgumentOutOfRangeException>(() => site.ConfigureLogisticsProfile(
            new CustomerSiteProfileInput(null, null, null, null, null, null, null, null, 0,
                false, false, false, null, null), Now.AddMinutes(2)));
    }

    [Fact]
    public void Customer_rejects_invalid_code_and_source_identifier()
    {
        Assert.Throws<ArgumentException>(() => Customer.Create(Guid.NewGuid(), "!", "Kramp", null, null, null, true, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Customer.Create(Guid.NewGuid(), "KRM", "Kramp", null, 0, null, true, Now));
    }
}
