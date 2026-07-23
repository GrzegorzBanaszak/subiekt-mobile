using SubiektMobile.Domain.Customers;
using Xunit;

namespace SubiektMobile.Domain.Tests;

public sealed class PackagingConfigurationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
    [Fact] public void Packaging_type_normalizes_code_and_validates_weights() { var type = PackagingType.Create(Guid.NewGuid(), " klt-42 ", "KLT 42", 1.2m, 20, true, Now); Assert.Equal("KLT-42", type.NormalizedCode); Assert.Throws<ArgumentOutOfRangeException>(() => PackagingType.Create(Guid.NewGuid(), "X", "X", -1, null, true, Now)); }
    [Fact] public void Part_mapping_normalizes_part_and_accepts_optional_engineering_change() { var mapping = CustomerPartMapping.Create(Guid.NewGuid(), Guid.NewGuid(), null, " p-100 ", 42, null, " E1 ", true, Now); Assert.Equal("P-100", mapping.NormalizedCustomerPartNumber); Assert.Equal("E1", mapping.EngineeringChange); }
}
