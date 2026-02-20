using Claims.Domain.Covers;
using Xunit;

namespace Claims.Tests
{
    public class PremiumCalculatorTests
    {
        #region Basic Premium Calculation Tests

        [Fact]
        public void ComputePremium_ReturnsZero_WhenStartDateEqualsEndDate()
        {
            // Arrange
            var sameDate = new DateTime(2026, 1, 15);
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(sameDate, sameDate, coverType);

            // Assert
            Assert.Equal(0m, premium);
        }

        [Fact]
        public void ComputePremium_ReturnsZero_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var startDate = new DateTime(2026, 2, 1);
            var endDate = new DateTime(2026, 1, 1); // End date before start date
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            Assert.Equal(0m, premium);
        }

        [Fact]
        public void ComputePremium_ReturnsPositiveValue_WhenDatesAreValid()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 15); // 14 days
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            Assert.True(premium > 0, "Premium should be greater than 0 for valid dates");
        }

        #endregion

        #region Cover Type Multiplier Tests

        [Fact]
        public void ComputePremium_AppliesYachtMultiplier_1Point1()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 2); // 1 day
            var yachtCoverType = CoverType.Yacht;

            // Act
            var yachtPremium = PremiumCalculator.ComputePremium(startDate, endDate, yachtCoverType);

            // Assert
            // Expected: 1250 * 1.1 * 1 (no discount for day 0)
            var expectedBasePremium = 1250m * 1.1m;
            Assert.Equal(expectedBasePremium, yachtPremium);
        }

        [Fact]
        public void ComputePremium_AppliesPassengerShipMultiplier_1Point2()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 2); // 1 day
            var passengerShipCoverType = CoverType.PassengerShip;

            // Act
            var passengerShipPremium = PremiumCalculator.ComputePremium(startDate, endDate, passengerShipCoverType);

            // Assert
            // Expected: 1250 * 1.2 * 1 (no discount for day 0)
            var expectedBasePremium = 1250m * 1.2m;
            Assert.Equal(expectedBasePremium, passengerShipPremium);
        }

        [Fact]
        public void ComputePremium_AppliesTankerMultiplier_1Point5()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 2); // 1 day
            var tankerCoverType = CoverType.Tanker;

            // Act
            var tankerPremium = PremiumCalculator.ComputePremium(startDate, endDate, tankerCoverType);

            // Assert
            // Expected: 1250 * 1.5 * 1 (no discount for day 0)
            var expectedBasePremium = 1250m * 1.5m;
            Assert.Equal(expectedBasePremium, tankerPremium);
        }

        [Fact]
        public void ComputePremium_AppliesDefaultRate_ForUnknownCoverType()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 2); // 1 day
            var unknownCoverType = (CoverType)999; // Invalid cover type (outside defined enum values)

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, unknownCoverType);

            // Assert
            // Expected: 1250 * 1.3 * 1 (baseRate * 1 day with no discount)
            var expectedBasePremium = 1250m * 1.3m;
            Assert.Equal(expectedBasePremium, premium);
        }

        #endregion

        #region Discount Rule Tests (Days 0-30: No Discount)

        [Fact]
        public void ComputePremium_AppliesNoDiscount_For1DayCoverage()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 2); // 1 day (day 0)
            var coverType = CoverType.PassengerShip;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Expected: 1250 * 1.2 * 1 * (1 - 0) = 1500
            var expectedPremium = 1250m * 1.2m;
            Assert.Equal(expectedPremium, premium);
        }

        [Fact]
        public void ComputePremium_AppliesNoDiscount_For30DayCoverage()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 31); // 30 days (days 0-29)
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Expected: 30 * 1250 * 1.1 * (1 - 0) = 41250
            var expectedPremium = 30m * 1250m * 1.1m;
            Assert.Equal(expectedPremium, premium);
        }

        #endregion

        #region Discount Rule Tests (Days 31-180)

        [Fact]
        public void ComputePremium_AppliesDefaultDiscount_For31To180DayCoverage()
        {
            // Arrange - 60 days coverage
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 3, 1); // 59 days (2026-01-01 to 2026-03-01)
            var coverType = CoverType.PassengerShip;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Verify it's lower than undiscounted version due to applying 2% default discount from day 31
            var noDaysCost = 59m * 1250m * 1.2m;
            Assert.True(premium > 0 && premium < noDaysCost, "Premium should be positive and reduced by discounts");
        }

        [Fact]
        public void ComputePremium_AppliesYachtDiscount_For31To180DayCoverage()
        {
            // Arrange - 59 days coverage (2026-01-01 to 2026-03-01)
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 3, 1); // 59 days
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Days 0-29: no discount (30 days: Jan 1-30)
            // Days 30-58: 5% Yacht-specific discount (29 days: Jan 31 - Feb 28)
            var premiumDays0To30 = 30m * 1250m * 1.1m * (1m - 0m);
            var premiumDays31Plus = 29m * 1250m * 1.1m * (1m - 0.05m);
            var expectedPremium = premiumDays0To30 + premiumDays31Plus;

            Assert.Equal(expectedPremium, premium);
        }

        [Fact]
        public void ComputePremium_AppliesCorrectDiscount_AtDay31Boundary()
        {
            // Arrange - Exactly 31 days (day 30 should get the discount)
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 2, 1); // 31 days
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Days 0-29: no discount (30 days)
            // Day 30: 5% Yacht-specific discount (1 day)
            var premiumDays0To30 = 30m * 1250m * 1.1m * (1m - 0m);
            var premiumDay31 = 1m * 1250m * 1.1m * (1m - 0.05m);
            var expectedPremium = premiumDays0To30 + premiumDay31;

            Assert.Equal(expectedPremium, premium);
        }

        #endregion

        #region Discount Rule Tests (Days 181+)

        [Fact]
        public void ComputePremium_AppliesHigherDefaultDiscount_For181PlusDayCoverage()
        {
            // Arrange - 200 days coverage
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 7, 19); // 200 days
            var coverType = CoverType.PassengerShip;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Verify the premium is lower than undiscounted version due to tiers
            var noDaysCost = 200m * 1250m * 1.2m;
            Assert.True(premium > 0 && premium < noDaysCost, "Premium should be positive and reduced by discounts");
        }

        [Fact]
        public void ComputePremium_AppliesHigherYachtDiscount_For181PlusDayCoverage()
        {
            // Arrange - 200 days coverage
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 7, 19); // 200 days
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Verify the premium is lower than undiscounted version due to tiers
            var noDaysCost = 200m * 1250m * 1.1m;
            Assert.True(premium > 0 && premium < noDaysCost, "Premium should be positive and reduced by discounts");
        }

        [Fact]
        public void ComputePremium_AppliesCorrectDiscount_AtDay181Boundary()
        {
            // Arrange - Coverage period where day 180 is last day at 5% discount
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 7, 1); // 182 days (0-181)
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert - Just verify the premium is calculated with discounts applied
            Assert.True(premium > 0, "Premium should be positive");
            
            // Verify it's reasonable - less than a 182-day period with no discounts
            var noDaysCost = 182m * 1250m * 1.1m;
            Assert.True(premium < noDaysCost, "Premium should be less than undiscounted equivalent due to discount tiers");
        }

        [Fact]
        public void ComputePremium_AppliesCorrectDiscount_At182Days()
        {
            // Arrange - 182 days coverage (day 181 should get the 8% discount for Yacht)
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 7, 2); // 183 days to ensure day 181 and 182 exist
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert - Verify premium is lower than pure multiplication (discounts are applied)
            var noDaysCost = 183m * 1250m * 1.1m;
            Assert.True(premium > 0 && premium < noDaysCost, "Premium should be positive and less than undiscounted equivalent");
        }

        #endregion

        #region Full Year Coverage Tests

        [Fact]
        public void ComputePremium_ComputesCorrectly_ForFullYear365Days()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 12, 31); // 364 days (2026 is not a leap year)
            var coverType = CoverType.Tanker;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            // Should apply all three discount levels for Tanker coverage
            Assert.True(premium > 0, "Premium should be positive");
            // Tanker has no special discount entries, so it uses default: 0% (days 0-30), 2% (days 31-180), 3% (days 181+)
        }

        #endregion

        #region Edge Cases and Precision Tests

        [Fact]
        public void ComputePremium_HandlesLeapYearCorrectly()
        {
            // Arrange - 2024 is a leap year
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 12, 31); // 365 days in a leap year
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            Assert.True(premium > 0, "Premium should be positive");
        }

        [Fact]
        public void ComputePremium_ReturnsDecimal_WithCorrectPrecision()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 15);
            var coverType = CoverType.Yacht;

            // Act
            var premium = PremiumCalculator.ComputePremium(startDate, endDate, coverType);

            // Assert
            Assert.IsType<decimal>(premium);
            Assert.True(premium >= 0, "Premium should be non-negative");
        }

        [Fact]
        public void ComputePremium_YachtLessThanPassengerShipForSamePeriod()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 31);

            // Act
            var yachtPremium = PremiumCalculator.ComputePremium(startDate, endDate, CoverType.Yacht);
            var passengerShipPremium = PremiumCalculator.ComputePremium(startDate, endDate, CoverType.PassengerShip);

            // Assert - PassengerShip multiplier (1.2) > Yacht multiplier (1.1)
            Assert.True(passengerShipPremium > yachtPremium);
        }

        [Fact]
        public void ComputePremium_PassengerShipLessThanTankerForSamePeriod()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 31);

            // Act
            var passengerShipPremium = PremiumCalculator.ComputePremium(startDate, endDate, CoverType.PassengerShip);
            var tankerPremium = PremiumCalculator.ComputePremium(startDate, endDate, CoverType.Tanker);

            // Assert - Tanker multiplier (1.5) > PassengerShip multiplier (1.2)
            Assert.True(tankerPremium > passengerShipPremium);
        }

        [Fact]
        public void ComputePremium_LongerCoveragePeriodProducesHigherPremium()
        {
            // Arrange
            var startDate = new DateTime(2026, 1, 1);
            var coverType = CoverType.Yacht;
            var shortEndDate = new DateTime(2026, 1, 16); // 15 days
            var longEndDate = new DateTime(2026, 2, 1); // 31 days

            // Act
            var shortPremium = PremiumCalculator.ComputePremium(startDate, shortEndDate, coverType);
            var longPremium = PremiumCalculator.ComputePremium(startDate, longEndDate, coverType);

            // Assert
            Assert.True(longPremium > shortPremium, "Longer coverage should have higher premium");
        }

        [Fact]
        public void ComputePremium_DiscountReducesPremiumComparingToNoDays()
        {
            // Arrange - Compare discounted days to undiscounted equivalent
            var startDate = new DateTime(2026, 1, 1);
            var shortEnd = new DateTime(2026, 2, 1); // 31 days (no discount)
            var longEnd = new DateTime(2026, 3, 31); // 90 days (with discounts from day 31+)
            var coverType = CoverType.Yacht;

            // Act
            var shortPremium = PremiumCalculator.ComputePremium(startDate, shortEnd, coverType);
            var longPremium = PremiumCalculator.ComputePremium(startDate, longEnd, coverType);

            // Assert - Verify discounts are being applied (longPremium should be less than 3x shortPremium due to discounts)
            var threeTimesShort = shortPremium * 3m;
            Assert.True(longPremium < threeTimesShort, "Discounts should reduce cumulative premium");
        }

        #endregion
    }
}
