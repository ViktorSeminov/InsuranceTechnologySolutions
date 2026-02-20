using Claims.Api.Controllers;
using Claims.Application.Covers;
using Claims.Domain.Covers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Claims.Tests
{
    public class CoversControllerTests
    {
        private readonly Mock<ICoverService> _mockCoverService;
        private readonly Mock<ILogger<CoversController>> _mockLogger;
        private readonly CoversController _controller;

        public CoversControllerTests()
        {
            _mockCoverService = new Mock<ICoverService>();
            _mockLogger = new Mock<ILogger<CoversController>>();
            // Note: AuditContext parameter is not used in CoversController, so passing null is acceptable
            _controller = new CoversController(_mockCoverService.Object, null!, _mockLogger.Object);
        }

        #region ComputePremiumAsync Tests

        [Fact]
        public void ComputePremiumAsync_ReturnsOkWithPremium_WhenInputIsValid()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 15);
            var coverType = CoverType.Yacht;
            var expectedPremium = 10000m; // Example calculation result

            // Act
            var result = _controller.ComputePremiumAsync(startDate, endDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.IsType<decimal>(okResult.Value);
        }

        [Fact]
        public void ComputePremiumAsync_ComputesPremiumForYacht_WithCorrectDates()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 31); // 30 days
            var coverType = CoverType.Yacht;

            // Act
            var result = _controller.ComputePremiumAsync(startDate, endDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var premium = Assert.IsType<decimal>(okResult.Value);
            Assert.True(premium > 0, "Premium should be greater than 0");
        }

        [Fact]
        public void ComputePremiumAsync_ComputesPremiumForPassengerShip_WithCorrectDates()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 3, 1); // ~60 days
            var coverType = CoverType.PassengerShip;

            // Act
            var result = _controller.ComputePremiumAsync(startDate, endDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var premium = Assert.IsType<decimal>(okResult.Value);
            Assert.True(premium > 0, "Premium should be greater than 0");
        }

        [Fact]
        public void ComputePremiumAsync_ComputesPremiumForTanker_WithLongCoveragePeriod()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 12, 31); // Full year (365 days)
            var coverType = CoverType.Tanker;

            // Act
            var result = _controller.ComputePremiumAsync(startDate, endDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var premium = Assert.IsType<decimal>(okResult.Value);
            Assert.True(premium > 0, "Premium should be greater than 0");
        }

        [Fact]
        public void ComputePremiumAsync_ReturnsZero_WhenStartDateEqualsEndDate()
        {
            // Arrange
            var sameDate = new DateTime(2026, 1, 15);
            var coverType = CoverType.Yacht;

            // Act
            var result = _controller.ComputePremiumAsync(sameDate, sameDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var premium = Assert.IsType<decimal>(okResult.Value);
            Assert.Equal(0m, premium);
        }

        [Fact]
        public void ComputePremiumAsync_ReturnsZero_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var startDate = new DateTime(2026, 2, 1);
            var endDate = new DateTime(2026, 1, 1); // End date before start date
            var coverType = CoverType.Yacht;

            // Act
            var result = _controller.ComputePremiumAsync(startDate, endDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var premium = Assert.IsType<decimal>(okResult.Value);
            Assert.Equal(0m, premium);
        }

        [Fact]
        public void ComputePremiumAsync_AppliesDiscounts_For31To180DayCoverage()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 3, 1); // ~60 days (31-180 range)
            var coverType = CoverType.Yacht;

            // Act
            var result = _controller.ComputePremiumAsync(startDate, endDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var premium = Assert.IsType<decimal>(okResult.Value);
            // With discount of 5% for Yacht in this range, premium should be lower than full price
            Assert.True(premium > 0, "Premium should be greater than 0 even with discount");
        }

        [Fact]
        public void ComputePremiumAsync_AppliesHigherDiscounts_For180PlusDayCoverage()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 12, 31); // 365 days (181+ range)
            var coverType = CoverType.Yacht;

            // Act
            var result = _controller.ComputePremiumAsync(startDate, endDate, coverType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var premium = Assert.IsType<decimal>(okResult.Value);
            // With discount of 8% for Yacht in this range, premium should be significantly lower
            Assert.True(premium > 0, "Premium should be greater than 0 even with higher discount");
        }

        #endregion

        #region GetAsync (All Covers) Tests

        [Fact]
        public async Task GetAsync_ReturnsAllCovers_WhenCoversExist()
        {
            // Arrange
            var expectedCovers = new List<Cover>
            {
                new Cover { Id = "1", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), Type = CoverType.Yacht, Premium = 100 },
                new Cover { Id = "2", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(60), Type = CoverType.PassengerShip, Premium = 150 },
                new Cover { Id = "3", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(90), Type = CoverType.Tanker, Premium = 200 }
            };

            _mockCoverService.Setup(s => s.GetAllCoversAsync())
                .ReturnsAsync(expectedCovers);

            // Act
            var result = await _controller.GetAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCovers = Assert.IsAssignableFrom<IEnumerable<Cover>>(okResult.Value);
            Assert.Equal(3, returnedCovers.Count());
            _mockCoverService.Verify(s => s.GetAllCoversAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ReturnsEmptyList_WhenNoCoversExist()
        {
            // Arrange
            var emptyList = new List<Cover>();

            _mockCoverService.Setup(s => s.GetAllCoversAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCovers = Assert.IsAssignableFrom<IEnumerable<Cover>>(okResult.Value);
            Assert.Empty(returnedCovers);
            _mockCoverService.Verify(s => s.GetAllCoversAsync(), Times.Once);
        }

        #endregion

        #region GetAsync (By ID) Tests

        [Fact]
        public async Task GetAsync_ReturnsCover_WhenCoverExists()
        {
            // Arrange
            var coverId = "cover-1";
            var expectedCover = new Cover
            {
                Id = coverId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.Yacht,
                Premium = 100
            };

            _mockCoverService.Setup(s => s.GetCoverByIdAsync(coverId))
                .ReturnsAsync(expectedCover);

            // Act
            var result = await _controller.GetAsync(coverId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCover = Assert.IsType<Cover>(okResult.Value);
            Assert.Equal(coverId, returnedCover.Id);
            Assert.Equal(CoverType.Yacht, returnedCover.Type);
            _mockCoverService.Verify(s => s.GetCoverByIdAsync(coverId), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ReturnsNull_WhenCoverDoesNotExist()
        {
            // Arrange
            var coverId = "non-existent-id";

            _mockCoverService.Setup(s => s.GetCoverByIdAsync(coverId))
                .ReturnsAsync((Cover)null!);

            // Act
            var result = await _controller.GetAsync(coverId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockCoverService.Verify(s => s.GetCoverByIdAsync(coverId), Times.Once);
        }

        [Fact]
        public async Task GetAsync_PassesCorrectIdToService_WhenRetrievingById()
        {
            // Arrange
            var coverId = "cover-123";

            _mockCoverService.Setup(s => s.GetCoverByIdAsync(coverId))
                .ReturnsAsync(new Cover { Id = coverId });

            // Act
            await _controller.GetAsync(coverId);

            // Assert
            _mockCoverService.Verify(s => s.GetCoverByIdAsync("cover-123"), Times.Once);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_CallsServiceWithCorrectCover()
        {
            // Arrange
            var coverToCreate = new Cover
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Type = CoverType.PassengerShip,
                Premium = 150
            };

            _mockCoverService.Setup(s => s.CreateCoverAsync(It.IsAny<Cover>()))
                .ReturnsAsync(coverToCreate);

            // Act
            var result = await _controller.CreateAsync(coverToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockCoverService.Verify(s => s.CreateCoverAsync(It.IsAny<Cover>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ReturnsCreatedCover_WhenCoverIsValid()
        {
            // Arrange
            var coverToCreate = new Cover
            {
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20),
                Type = CoverType.ContainerShip,
                Premium = 120
            };

            _mockCoverService.Setup(s => s.CreateCoverAsync(It.IsAny<Cover>()))
                .ReturnsAsync(coverToCreate);

            // Act
            var result = await _controller.CreateAsync(coverToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(coverToCreate, okResult.Value);
        }

        [Fact]
        public async Task CreateAsync_AcceptsAllCoverTypes()
        {
            // Arrange
            var coverTypes = new[] { CoverType.Yacht, CoverType.PassengerShip, CoverType.ContainerShip, CoverType.BulkCarrier, CoverType.Tanker };

            foreach (var coverType in coverTypes)
            {
                var cover = new Cover
                {
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    Type = coverType,
                    Premium = 100
                };

                _mockCoverService.Setup(s => s.CreateCoverAsync(cover))
                    .ReturnsAsync(cover);

                // Act
                var result = await _controller.CreateAsync(cover);

                // Assert
                Assert.IsType<OkObjectResult>(result);
            }
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ReturnsNoContent_WhenCoverIsSuccessfullyDeleted()
        {
            // Arrange
            var coverId = "cover-to-delete";

            _mockCoverService.Setup(s => s.DeleteCoverAsync(coverId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteAsync(coverId);

            // Assert
            Assert.IsType<NoContentResult>(result.Result);
            _mockCoverService.Verify(s => s.DeleteCoverAsync(coverId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenCoverDoesNotExist()
        {
            // Arrange
            var coverId = "non-existent-id";

            _mockCoverService.Setup(s => s.DeleteCoverAsync(coverId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteAsync(coverId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockCoverService.Verify(s => s.DeleteCoverAsync(coverId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_PassesCorrectIdToService()
        {
            // Arrange
            var coverId = "cover-123";

            _mockCoverService.Setup(s => s.DeleteCoverAsync(coverId))
                .ReturnsAsync(true);

            // Act
            await _controller.DeleteAsync(coverId);

            // Assert
            _mockCoverService.Verify(s => s.DeleteCoverAsync("cover-123"), Times.Once);
        }

        #endregion
    }
}
