using Claims.Application.Common.Events;
using Claims.Domain.Covers;
using Claims.Domain.Events;
using Claims.Domain.Validators;
using Claims.Infrastructure.Claims;
using Microsoft.EntityFrameworkCore;


namespace Claims.Application.Covers
{
    public class CoverService: ICoverService
    {
        private readonly ClaimsContext _claimsContext;
        private readonly IEventDispatcher _dispatcher;

        public CoverService(ClaimsContext claimsContext, IEventDispatcher dispatcher)
        {
            _claimsContext = claimsContext;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Asynchronously retrieves all cover records from the data store.
        /// </summary>
        /// <remarks>This method performs the operation asynchronously, which can help prevent blocking
        /// the calling thread in UI or scalable server applications.</remarks>
        /// <returns>A collection of <see cref="Cover"/> objects representing all covers in the database. The collection is empty
        /// if no covers are found.</returns>
        public async Task<IEnumerable<Cover>> GetAllCoversAsync()
        {
            var results = await _claimsContext.Covers.ToListAsync();
            return results;
        }

        /// <summary>
        /// Asynchronously retrieves a cover with the specified identifier.
        /// </summary>
        /// <remarks>This method queries all covers from the data context and searches for a cover with
        /// the matching identifier. To avoid unnecessary database queries, ensure that the identifier provided is
        /// valid.</remarks>
        /// <param name="id">The unique identifier of the cover to retrieve. This parameter cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cover associated with the
        /// specified identifier, or null if no cover is found.</returns>
        public async Task<Cover?> GetCoverByIdAsync(string id)
        {
            return await _claimsContext.Covers
                .Where(cover => cover.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new insurance cover, assigns a unique identifier, calculates the premium, and saves the cover to
        /// the database asynchronously.
        /// </summary>
        /// <remarks>This method validates the input cover before creation and dispatches an audit event
        /// after saving the cover. Ensure that the cover's start and end dates are valid to avoid validation
        /// errors.</remarks>
        /// <param name="cover">The cover object containing the details of the insurance cover to create. Must be valid according to the
        /// CoverValidator.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created cover, including its
        /// unique identifier and calculated premium.</returns>
        public async Task<Cover> CreateCoverAsync(Cover cover)
        {
            CoverValidator.Validate(cover);

            cover.Id = Guid.NewGuid().ToString();
            cover.Premium = PremiumCalculator.ComputePremium(cover.StartDate, cover.EndDate, cover.Type);

            _claimsContext.Covers.Add(cover);
            await _claimsContext.SaveChangesAsync();

            _ = _dispatcher.DispatchAsync(
                new AuditEvent(
                    AuditEntityType.Cover,
                    cover.Id,
                    AuditAction.POST,
                    DateTime.UtcNow));

            return cover;
        }

        /// <summary>
        /// Deletes the cover with the specified identifier from the database and logs the deletion action as an audit
        /// event.
        /// </summary>
        /// <remarks>If no cover with the specified identifier exists, the method completes without making
        /// any changes. An audit event is always logged, regardless of whether a cover was found and deleted.</remarks>
        /// <param name="id">The unique identifier of the cover to delete. This value cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is always <see langword="true"/>,
        /// indicating that the delete operation was initiated.</returns>
        public async Task<bool> DeleteCoverAsync(string id)
        {

            _ = _dispatcher.DispatchAsync(
                new AuditEvent(
                    AuditEntityType.Cover,
                    id,
                    AuditAction.DELETE,
                    DateTime.UtcNow));

            var cover = await _claimsContext.Covers.Where(cover => cover.Id == id).SingleOrDefaultAsync();
            if (cover is not null)
            {
                _claimsContext.Covers.Remove(cover);
                await _claimsContext.SaveChangesAsync();
            }

            return true;
        }
    }
}
