using SubiektMobile.Infrastructure.Identity;
using Xunit;

namespace SubiektMobile.Infrastructure.IntegrationTests;

public sealed class TemporaryPasswordGeneratorTests
{
    [Fact]
    public void Generated_passwords_have_required_length_and_character_groups()
    {
        var generator = new TemporaryPasswordGenerator();

        for (var index = 0; index < 100; index++)
        {
            var password = generator.Generate();

            Assert.Equal(20, password.Length);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, character => "!@#$%*-_.".Contains(character));
        }
    }
}
