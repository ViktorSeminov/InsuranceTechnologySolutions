using Claims.Application.Claims;
using Claims.Domain.Claims;
using Claims.Domain.Covers;
using Claims.Infrastructure.Claims;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using Xunit;
using Claims.Application.Common.Events;
using Claims.Domain.Events;

namespace Claims.Tests
{
    public class ClaimServiceTests
    {
        private readonly ClaimsContext _claimsContext;
        private readonly Mock<IEventDispatcher> _mockEventDispatcher;
        private readonly ClaimService _claimService;

        public ClaimServiceTests()
        {
            // Use real in-memory database instead of mocking DbContext
            var options = new DbContextOptionsBuilder<ClaimsContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _claimsContext = new ClaimsContext(options);
            _mockEventDispatcher = new Mock<IEventDispatcher>();
            _claimService = new ClaimService(_claimsContext, _mockEventDispatcher.Object);
            
            _mockEventDispatcher.Setup(m => m.DispatchAsync(It.IsAny<AuditEvent>()))
                .Returns(Task.CompletedTask);
        }

        #region CreateClaimAsync - Validation Tests

        [Fact]
        public async Task CreateClaimAsync_ThrowsArgumentException_WhenDamageCostExceeds100000()
        {
            // Arrange
            var invalidClaim = new Claim
            {
                CoverId = "cover-1",
                Name = "Invalid Claim",
                Type = ClaimType.Collision,
                DamageCost = 100001, // Exceeds the limit
                Created = DateTime.UtcNow
            };

            var cover = new Cover
            {
                Id = "cover-1",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.Yacht,
                Premium = 100
            };

            _claimsContext.Covers.Add(cover);
            await _claimsContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _claimService.CreateClaimAsync(invalidClaim));
            
            Assert.Contains("Damage cost cannot exceed 100,000", exception.Message);
        }

        [Fact]
        public async Task CreateClaimAsync_ThrowsArgumentException_WhenClaimDateIsBeforeCoverStartDate()
        {
            // Arrange
            var coverStartDate = DateTime.UtcNow.AddDays(10);
            var claimCreatedDate = coverStartDate.AddDays(-1);

            var invalidClaim = new Claim
            {
                CoverId = "cover-3",
                Name = "Out of Period Claim",
                Type = ClaimType.Collision,
                DamageCost = 50000,
                Created = claimCreatedDate
            };

            var cover = new Cover
            {
                Id = "cover-3",
                StartDate = coverStartDate,
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.ContainerShip,
                Premium = 100
            };

            _claimsContext.Covers.Add(cover);
            await _claimsContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _claimService.CreateClaimAsync(invalidClaim));

            Assert.Contains("Claim created date must be within the cover period", exception.Message);
        }

        [Fact]
        public async Task CreateClaimAsync_ThrowsArgumentException_WhenClaimDateIsAfterCoverEndDate()
        {
            // Arrange
            var coverEndDate = DateTime.UtcNow.AddDays(-10);
            var claimCreatedDate = coverEndDate.AddDays(1);

            var invalidClaim = new Claim
            {
                CoverId = "cover-4",
                Name = "Post Period Claim",
                Type = ClaimType.BadWeather,
                DamageCost = 25000,
                Created = claimCreatedDate
            };

            var cover = new Cover
            {
                Id = "cover-4",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = coverEndDate,
                Type = CoverType.BulkCarrier,
                Premium = 75
            };

            _claimsContext.Covers.Add(cover);
            await _claimsContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _claimService.CreateClaimAsync(invalidClaim));

            Assert.Contains("Claim created date must be within the cover period", exception.Message);
        }

        [Fact]
        public async Task CreateClaimAsync_ThrowsException_WhenCoverDoesNotExist()
        {
            // Arrange
            var claim = new Claim
            {
                CoverId = "non-existent-cover",
                Name = "Claim with Invalid Cover",
                Type = ClaimType.Collision,
                DamageCost = 50000,
                Created = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _claimService.CreateClaimAsync(claim));

            Assert.Contains("Cover with id non-existent-cover does not exist", exception.Message);
        }

        [Fact]
        public async Task CreateClaimAsync_SuccessfullyCreates_WhenClaimIsValid()
        {
            // Arrange
            var claimToCreate = new Claim
            {
                CoverId = "cover-6",
                Name = "Valid Claim",
                Type = ClaimType.Fire,
                DamageCost = 50000,
                Created = DateTime.UtcNow
            };

            var cover = new Cover
            {
                Id = "cover-6",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.Tanker,
                Premium = 100
            };

            _claimsContext.Covers.Add(cover);
            await _claimsContext.SaveChangesAsync();

            // Act
            var result = await _claimService.CreateClaimAsync(claimToCreate);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal(claimToCreate.CoverId, result.CoverId);
            Assert.Equal(claimToCreate.Name, result.Name);
            Assert.Equal(claimToCreate.DamageCost, result.DamageCost);

            // Verify the claim was saved
            var savedClaim = await _claimsContext.Claims.FirstOrDefaultAsync(c => c.Id == result.Id);
            Assert.NotNull(savedClaim);

            // Verify audit event was dispatched
            _mockEventDispatcher.Verify(m => m.DispatchAsync(It.Is<AuditEvent>(e =>
                e.EntityType == AuditEntityType.Claim &&
                e.Action == AuditAction.POST)), Times.Once);
        }

        [Fact]
        public async Task CreateClaimAsync_ValidatesWithMaxAllowedDamageCost()
        {
            // Arrange - Test boundary: exactly at the limit (100,000 should be allowed)
            var validClaim = new Claim
            {
                CoverId = "cover-7",
                Name = "Max Valid Claim",
                Type = ClaimType.Collision,
                DamageCost = 100000,
                Created = DateTime.UtcNow
            };

            var cover = new Cover
            {
                Id = "cover-7",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.Yacht,
                Premium = 100
            };

            _claimsContext.Covers.Add(cover);
            await _claimsContext.SaveChangesAsync();

            // Act
            var result = await _claimService.CreateClaimAsync(validClaim);

            // Assert - Should not throw and should be created successfully
            Assert.NotNull(result);
            Assert.Equal(100000, result.DamageCost);
        }

        [Fact]
        public async Task CreateClaimAsync_ValidatesClaimOnBoundaryDates()
        {
            // Arrange - Claim created on exactly the cover start date
            var coverStartDate = DateTime.UtcNow.AddDays(-10);
            var coverEndDate = DateTime.UtcNow.AddDays(10);

            var validClaim = new Claim
            {
                CoverId = "cover-8",
                Name = "Boundary Start Date Claim",
                Type = ClaimType.Fire,
                DamageCost = 50000,
                Created = coverStartDate
            };

            var cover = new Cover
            {
                Id = "cover-8",
                StartDate = coverStartDate,
                EndDate = coverEndDate,
                Type = CoverType.PassengerShip,
                Premium = 100
            };

            _claimsContext.Covers.Add(cover);
            await _claimsContext.SaveChangesAsync();

            // Act
            var result = await _claimService.CreateClaimAsync(validClaim);

            // Assert - Should not throw
            Assert.NotNull(result);
        }

        #endregion

        #region GetAllClaimsAsync Tests

        [Fact]
        public async Task GetAllClaimsAsync_ReturnsAllClaims()
        {
            // Arrange
            var cover = new Cover
            {
                Id = "cover-test",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.Yacht,
                Premium = 100
            };

            var claims = new List<Claim>
            {
                new Claim { Id = "1", CoverId = "cover-test", Name = "Claim 1", Type = ClaimType.Collision, DamageCost = 1000, Created = DateTime.UtcNow },
                new Claim { Id = "2", CoverId = "cover-test", Name = "Claim 2", Type = ClaimType.Fire, DamageCost = 500, Created = DateTime.UtcNow }
            };

            _claimsContext.Covers.Add(cover);
            _claimsContext.Claims.AddRange(claims);
            await _claimsContext.SaveChangesAsync();

            // Act
            var result = await _claimService.GetAllClaimsAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.NotEmpty(result);
        }

        #endregion

        #region GetClaimByIdAsync Tests

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsClaim_WhenExists()
        {
            // Arrange
            var cover = new Cover
            {
                Id = "cover-test",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.Yacht,
                Premium = 100
            };

            var claim = new Claim
            {
                Id = "test-id",
                CoverId = "cover-test",
                Name = "Test Claim",
                Type = ClaimType.Collision,
                DamageCost = 1000,
                Created = DateTime.UtcNow
            };

            _claimsContext.Covers.Add(cover);
            _claimsContext.Claims.Add(claim);
            await _claimsContext.SaveChangesAsync();

            // Act
            var result = await _claimService.GetClaimByIdAsync("test-id");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-id", result.Id);
            Assert.Equal("Test Claim", result.Name);
        }

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsNull_WhenClaimDoesNotExist()
        {
            // Act
            var result = await _claimService.GetClaimByIdAsync("non-existent-id");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteClaimAsync Tests

        [Fact]
        public async Task DeleteClaimAsync_DeletesClaim_WhenExists()
        {
            // Arrange
            var cover = new Cover
            {
                Id = "cover-test",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.Yacht,
                Premium = 100
            };

            var claim = new Claim
            {
                Id = "claim-to-delete",
                CoverId = "cover-test",
                Name = "Claim to Delete",
                Type = ClaimType.BadWeather,
                DamageCost = 1000,
                Created = DateTime.UtcNow
            };

            _claimsContext.Covers.Add(cover);
            _claimsContext.Claims.Add(claim);
            await _claimsContext.SaveChangesAsync();

            // Act
            var result = await _claimService.DeleteClaimAsync("claim-to-delete");

            // Assert
            Assert.True(result);
            var deletedClaim = await _claimsContext.Claims.FirstOrDefaultAsync(c => c.Id == "claim-to-delete");
            Assert.Null(deletedClaim);

            // Verify audit event was dispatched
            _mockEventDispatcher.Verify(m => m.DispatchAsync(It.Is<AuditEvent>(e =>
                e.EntityType == AuditEntityType.Claim &&
                e.Action == AuditAction.DELETE)), Times.Once);
        }

        [Fact]
        public async Task DeleteClaimAsync_ReturnsTrue_WhenClaimDoesNotExist()
        {
            // Act - The service returns true even if claim doesn't exist
            var result = await _claimService.DeleteClaimAsync("non-existent-id");

            // Assert
            Assert.True(result);

            // Verify audit event was still dispatched
            _mockEventDispatcher.Verify(m => m.DispatchAsync(It.Is<AuditEvent>(e =>
                e.EntityType == AuditEntityType.Claim &&
                e.Action == AuditAction.DELETE)), Times.Once);
        }

        #endregion
    }
}
