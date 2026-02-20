using Org.BouncyCastle.Math.EC.Multiplier;

namespace Claims.Domain.Covers
{
    public static class PremiumCalculator
    {
        private static decimal baseDayRate = 1250m;
        private static decimal baseRate = 1.3m;

        public sealed record DiscountRule
        {
            public int FromDay { get; init; }
            public int? ToDay { get; init; }
            public IReadOnlyDictionary<CoverType, decimal> Discounts { get; init; } = new Dictionary<CoverType, decimal>();
            public decimal DefaultDiscount { get; init; }
        }

        private static readonly IReadOnlyDictionary<CoverType, decimal> Multipliers =
            new Dictionary<CoverType, decimal>
            {
                [CoverType.Yacht] = 1.1m,
                [CoverType.PassengerShip] = 1.2m,
                [CoverType.Tanker] = 1.5m
            };

        private static readonly IReadOnlyList<DiscountRule> DiscountRules = new List<DiscountRule>
        {
            // First 30 days: day index 0..29 (no discount)
            new DiscountRule
            {
                FromDay = 0,
                ToDay = 29,
                DefaultDiscount = 0m,
                Discounts = new Dictionary<CoverType, decimal>()
            },

            // Next 150 days: day index 30..179 (5% for Yacht, 2% for others)
            new DiscountRule
            {
                FromDay = 30,
                ToDay = 179,
                DefaultDiscount = 0.02m,
                Discounts = new Dictionary<CoverType, decimal>
                {
                    [CoverType.Yacht] = 0.05m
                }
            },

            // Remaining days: day index 180+ (additional discounts => Yacht 8% total, others 3% total)
            new DiscountRule
            {
                FromDay = 180,
                ToDay = null,
                DefaultDiscount = 0.03m,
                Discounts = new Dictionary<CoverType, decimal>
                {
                    [CoverType.Yacht] = 0.08m
                }
            }
        };

        private static decimal GetDiscountForDay(int day, CoverType coverType)
        {
            var discount = DiscountRules.Single(p => day >= p.FromDay &&
                                                (p.ToDay == null || day <= p.ToDay));

            return discount.Discounts.TryGetValue(coverType, out var currentDiscount)
                    ? currentDiscount
                    : discount.DefaultDiscount;
        }

        public static decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
        {
            var insuranceLength = (endDate - startDate).TotalDays;
            if (insuranceLength <= 0) return 0m;

            var multiplier = Multipliers.GetValueOrDefault(coverType, baseRate);
            var premiumPerDay = baseDayRate * multiplier;

            decimal totalPremium = 0m;

            for (var day = 0; day < insuranceLength; day++)
            {
                var discount = GetDiscountForDay(day, coverType);
                totalPremium += premiumPerDay * (1 - discount);
            }

            return totalPremium;
        }
    }
}
