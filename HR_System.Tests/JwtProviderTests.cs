using HR_System.Models.Entities;
using HR_System.Providers;
using HR_System.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Xunit;

namespace HR_System.Tests
{
    public class JwtProviderTests
    {
        private readonly IJwtProvider _jwtProvider;

        private const string TestKey = "SuperSecretKeyThatIsAtLeast32CharactersLong123!";
        private const string TestIssuer = "HR_System_Backend";
        private const string TestAudience = "HR_System_Frontend";

        public JwtProviderTests()
        {
            // Build in-memory configuration mimicking appsettings.json
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Jwt:Key", TestKey },
                { "Jwt:Issuer", TestIssuer },
                { "Jwt:Audience", TestAudience }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _jwtProvider = new JwtProvider(configuration);
        }

        [Fact]
        public void ReturnsNonEmptyToken()
        {
            // Arrange
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                Email = "john.doe@example.com",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var token = _jwtProvider.GenerateToken(employee);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void ContainsCorrectSubjectandEmailClaims()
        {
            // Arrange
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                Email = "alice@example.com",
                FirstName = "Alice",
                LastName = "Smith"
            };

            // Act
            var token = _jwtProvider.GenerateToken(employee);
            var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

            // Assert
            var subClaim = parsedToken.Claims
                .First(c => c.Type == JwtRegisteredClaimNames.Sub);
            var emailClaim = parsedToken.Claims
                .First(c => c.Type == JwtRegisteredClaimNames.Email);

            Assert.Equal(employee.Id.ToString(), subClaim.Value);
            Assert.Equal(employee.Email, emailClaim.Value);
        }

        [Fact]
        public void ExpiresInTwoHours()
        {
            // Arrange
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                Email = "expiry@example.com",
                FirstName = "Time",
                LastName = "Test"
            };

            var beforeGeneration = DateTime.UtcNow;

            // Act
            var token = _jwtProvider.GenerateToken(employee);
            var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

            // Assert
            var expectedExpiry = beforeGeneration.AddHours(2);
            var actualExpiry = parsedToken.ValidTo;

            var difference = (actualExpiry - expectedExpiry).Duration();
            Assert.True(difference.TotalSeconds < 5,
                $"Expiry should be within 5 seconds of expected ({expectedExpiry}); actual: {actualExpiry}");
        }
    }
}