using Claims.Api.Controllers;
using Claims.Application.Claims;
using Claims.Domain.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Claims.Tests
{
    public class ClaimsControllerTests
    {
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<ILogger<ClaimsController>> _mockLogger;
        private readonly ClaimsController _controller;

        public ClaimsControllerTests()
        {
            _mockClaimsService = new Mock<IClaimsService>();
            _mockLogger = new Mock<ILogger<ClaimsController>>();
            _controller = new ClaimsController(_mockLogger.Object, _mockClaimsService.Object);
        }

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_ReturnsAllClaims_WhenClaimsExist()
        {
            // Arrange
            var expectedClaims = new List<Claim>
            {
                new Claim { Id = "1", CoverId = "cover-1", Name = "Claim 1", Type = ClaimType.Collision, DamageCost = 100, Created = DateTime.UtcNow },
                new Claim { Id = "2", CoverId = "cover-2", Name = "Claim 2", Type = ClaimType.Fire, DamageCost = 50, Created = DateTime.UtcNow },
                new Claim { Id = "3", CoverId = "cover-3", Name = "Claim 3", Type = ClaimType.Grounding, DamageCost = 30, Created = DateTime.UtcNow }
            };

            _mockClaimsService.Setup(s => s.GetAllClaimsAsync())
                .ReturnsAsync(expectedClaims);

            // Act
            var result = await _controller.GetAsync();

            // Assert
            var claimsList = result.ToList();
            Assert.Equal(3, claimsList.Count);
            Assert.Equal(expectedClaims, claimsList);
            _mockClaimsService.Verify(s => s.GetAllClaimsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ReturnsEmptyList_WhenNoClaimsExist()
        {
            // Arrange
            var emptyList = new List<Claim>();
            _mockClaimsService.Setup(s => s.GetAllClaimsAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetAsync();

            // Assert
            Assert.Empty(result);
            _mockClaimsService.Verify(s => s.GetAllClaimsAsync(), Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ReturnsClaim_WhenClaimExists()
        {
            // Arrange
            var claimId = "claim-123";
            var expectedClaim = new Claim
            {
                Id = claimId,
                CoverId = "cover-1",
                Name = "Test Claim",
                Type = ClaimType.Collision,
                DamageCost = 500,
                Created = DateTime.UtcNow
            };

            _mockClaimsService.Setup(s => s.GetClaimByIdAsync(claimId))
                .ReturnsAsync(expectedClaim);

            // Act
            var result = await _controller.GetByIdAsync(claimId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedClaim = Assert.IsType<Claim>(okResult.Value);
            Assert.Equal(expectedClaim.Id, returnedClaim.Id);
            Assert.Equal(expectedClaim.Type, returnedClaim.Type);
            Assert.Equal(expectedClaim.DamageCost, returnedClaim.DamageCost);
            _mockClaimsService.Verify(s => s.GetClaimByIdAsync(claimId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_WhenClaimDoesNotExist()
        {
            // Arrange
            var claimId = "non-existent-id";
            _mockClaimsService.Setup(s => s.GetClaimByIdAsync(claimId))
                .ReturnsAsync((Claim?)null);

            // Act
            var result = await _controller.GetByIdAsync(claimId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockClaimsService.Verify(s => s.GetClaimByIdAsync(claimId), Times.Once);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ReturnsCreatedClaim_WhenClaimIsValid()
        {
            // Arrange
            var newClaim = new Claim
            {
                Id = "new-claim-id",
                CoverId = "cover-1",
                Name = "New Claim",
                Type = ClaimType.Fire,
                DamageCost = 200,
                Created = DateTime.UtcNow
            };

            _mockClaimsService.Setup(s => s.CreateClaimAsync(newClaim))
                .ReturnsAsync(newClaim);

            // Act
            var result = await _controller.CreateAsync(newClaim);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedClaim = Assert.IsType<Claim>(okResult.Value);
            Assert.Equal(newClaim.Id, returnedClaim.Id);
            Assert.Equal(newClaim.Type, returnedClaim.Type);
            Assert.Equal(newClaim.DamageCost, returnedClaim.DamageCost);
            _mockClaimsService.Verify(s => s.CreateClaimAsync(newClaim), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var claimToCreate = new Claim
            {
                CoverId = "cover-1",
                Name = "Test Claim",
                Type = ClaimType.BadWeather,
                DamageCost = 150,
                Created = DateTime.UtcNow
            };

            _mockClaimsService.Setup(s => s.CreateClaimAsync(It.IsAny<Claim>()))
                .ReturnsAsync(claimToCreate);

            // Act
            await _controller.CreateAsync(claimToCreate);

            // Assert
            _mockClaimsService.Verify(
                s => s.CreateClaimAsync(It.Is<Claim>(c => 
                    c.CoverId == claimToCreate.CoverId && 
                    c.Type == claimToCreate.Type && 
                    c.DamageCost == claimToCreate.DamageCost)),
                Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ReturnsNoContent_WhenClaimIsSuccessfullyDeleted()
        {
            // Arrange
            var claimId = "claim-to-delete";
            _mockClaimsService.Setup(s => s.DeleteClaimAsync(claimId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteAsync(claimId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockClaimsService.Verify(s => s.DeleteClaimAsync(claimId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenClaimDoesNotExist()
        {
            // Arrange
            var claimId = "non-existent-claim";
            _mockClaimsService.Setup(s => s.DeleteClaimAsync(claimId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteAsync(claimId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockClaimsService.Verify(s => s.DeleteClaimAsync(claimId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_PassesCorrectIdToService()
        {
            // Arrange
            var claimId = "specific-claim-id";
            _mockClaimsService.Setup(s => s.DeleteClaimAsync(claimId))
                .ReturnsAsync(true);

            // Act
            await _controller.DeleteAsync(claimId);

            // Assert
            _mockClaimsService.Verify(s => s.DeleteClaimAsync(claimId), Times.Once);
        }

        #endregion
    }
}
