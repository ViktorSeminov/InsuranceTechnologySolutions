using Claims.Application.Common.Events;
using Claims.Domain.Events;
using Claims.Domain.Validators;
using Claims.Infrastructure.Claims;
using Microsoft.EntityFrameworkCore;
using Claim = Claims.Domain.Claims.Claim;

namespace Claims.Application.Claims
{
    public class ClaimService: IClaimsService
    {
        private readonly ClaimsContext _claimsContext;
        private readonly IEventDispatcher _dispatcher;

        public ClaimService(ClaimsContext claimsContext, IEventDispatcher dispatcher)
        {
            _claimsContext = claimsContext;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Asynchronously retrieves all claims from the data store.
        /// </summary>
        /// <remarks>This method executes the query asynchronously, which can help prevent blocking the
        /// calling thread in UI or scalable server applications.</remarks>
        /// <returns>A collection of <see cref="Claim"/> objects representing all claims in the data store.</returns>
        public async Task<IEnumerable<Claim>> GetAllClaimsAsync()
        {
            return await _claimsContext.Claims.ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves a claim that matches the specified unique identifier.
        /// </summary>
        /// <remarks>This method queries the underlying data store for a claim with the given identifier.
        /// Ensure that the provided identifier is valid to avoid unexpected results.</remarks>
        /// <param name="id">The unique identifier of the claim to retrieve. This parameter cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the claim associated with the
        /// specified identifier, or null if no matching claim is found.</returns>
        public async Task<Claim?> GetClaimByIdAsync(string id)
        {
            var result = await _claimsContext.Claims
                    .Where(claim => claim.Id == id)
                    .SingleOrDefaultAsync();

            return result;
        }

        /// <summary>
        /// Creates a new claim and saves it to the database after validating the claim and its associated cover.
        /// </summary>
        /// <remarks>This method performs validation on the claim and its associated cover before
        /// creation. The creation action is also audited.</remarks>
        /// <param name="claim">The claim to create. Must include a valid CoverId and meet all validation requirements.</param>
        /// <returns>The newly created Claim object, including its assigned identifier.</returns>
        /// <exception cref="Exception">Thrown if the cover specified by the claim's CoverId does not exist.</exception>
        public async Task<Claim> CreateClaimAsync(Claim claim)
        {
            //Validate claim
            ClaimValidator.Validate(claim);

            var cover = _claimsContext.Covers.Where(cover => cover.Id == claim.CoverId)
                .SingleOrDefault() ?? throw new Exception($"Cover with id {claim.CoverId} does not exist.");

            ClaimValidator.ValidateAgainstCover(claim, cover);

            claim.Id = Guid.NewGuid().ToString();
            _claimsContext.Claims.Add(claim);
            await _claimsContext.SaveChangesAsync();

            _ = _dispatcher.DispatchAsync(
                new AuditEvent(
                    AuditEntityType.Claim,
                    claim.Id,
                    AuditAction.POST,
                    DateTime.UtcNow));

            return claim;
        }

        /// <summary>
        /// Deletes the claim with the specified identifier from the claims context asynchronously.
        /// </summary>
        /// <remarks>If a claim with the specified identifier does not exist, no action is taken. An audit
        /// event is logged for the deletion attempt regardless of whether the claim was found.</remarks>
        /// <param name="id">The unique identifier of the claim to delete. This parameter cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is always <see langword="true"/>,
        /// indicating that the deletion process was initiated.</returns>
        public async Task<bool> DeleteClaimAsync(string id)
        {
            _ = _dispatcher.DispatchAsync(
                new AuditEvent(
                    AuditEntityType.Claim,
                    id,
                    AuditAction.DELETE,
                    DateTime.UtcNow));

            var claim = await GetClaimByIdAsync(id);

            if (claim is not null)
            {
                _claimsContext.Claims.Remove(claim);
                await _claimsContext.SaveChangesAsync();
            }

            return true;
        }
    }
}
