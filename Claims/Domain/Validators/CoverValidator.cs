using Claims.Domain.Covers;

namespace Claims.Domain.Validators
{
    public static class CoverValidator
    {
        public static void Validate(Cover cover)
        {
            if (cover.StartDate < DateTime.Now)
            {
                throw new ArgumentException("StartDate cannot be in the past");
            }

            if ((cover.EndDate - cover.StartDate).TotalDays > 365)
            {
                throw new ArgumentException("Cover duration cannot exceed one year.");
            }
        }
    }
}
