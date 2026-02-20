using Claims.Domain.Claims;
using Claims.Domain.Covers;

namespace Claims.Domain.Validators
{
    public static class ClaimValidator
    {
        public static void Validate(Claim claim)
        {
            if (claim.DamageCost > 100000)
                throw new ArgumentException("Damage cost cannot exceed 100,000.");
        }

        public static void ValidateAgainstCover(Claim claim, Cover cover)
        {
            if (claim.Created < cover.StartDate
                || claim.Created > cover.EndDate)
            {
                throw new ArgumentException("Claim created date must be within the cover period.");
            }
        }
    }
}
