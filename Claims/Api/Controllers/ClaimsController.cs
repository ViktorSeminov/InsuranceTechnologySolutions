using Claims.Application.Claims;
using Claims.Domain.Claims;
using Microsoft.AspNetCore.Mvc;


namespace Claims.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly ILogger<ClaimsController> _logger;
        private readonly IClaimsService _claimsService;

        public ClaimsController(ILogger<ClaimsController> logger, IClaimsService claimsService)
        {
            _logger = logger;
            _claimsService = claimsService;
        }

        /// <summary>
        /// Retrieves all claims asynchronously.
        /// </summary>
        /// <remarks>This method used to obtain the complete list of claims from the
        /// underlying data source. Ensure that the claims service is properly configured before calling this
        /// method.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <see
        /// cref="Claim"/> objects representing all claims.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Claim>))]
        [Produces("application/json")]
        public async Task<IEnumerable<Claim>> GetAsync()
        {
            return await _claimsService.GetAllClaimsAsync();
        }

        /// <summary>
        /// Retrieves a claim by its unique identifier.
        /// </summary>
        /// <remarks>This method asynchronously fetches the claim from the service. If the claim does not
        /// exist, it returns a 404 Not Found response.</remarks>
        /// <param name="id">The unique identifier of the claim to retrieve. This value cannot be null or empty.</param>
        /// <returns>An ActionResult containing the requested Claim if found; otherwise, a NotFound result.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Claim))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<ActionResult<Claim>> GetByIdAsync(string id)
        {
            var claim = await _claimsService.GetClaimByIdAsync(id);

            if (claim == null)
            {
                return NotFound();
            }

            return Ok(claim);
        }


        /// <summary>
        /// Creates a new claim and returns the created claim in the response.
        /// </summary>
        /// <remarks>This method persists the provided claim using the claims service. The claim parameter
        /// must be valid and properly populated before calling this method.</remarks>
        /// <param name="claim">The claim to create. Must not be null.</param>
        /// <returns>An ActionResult containing the created Claim object.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Claim))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<ActionResult<Claim>> CreateAsync(Claim claim)
        {
            var result = await _claimsService.CreateClaimAsync(claim);
            return Ok(result);
        }

        /// <summary>
        /// Deletes the claim associated with the specified identifier.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to delete a claim. Ensure that the
        /// provided identifier corresponds to an existing claim before calling this method.</remarks>
        /// <param name="id">The unique identifier of the claim to delete. This value must not be null or empty.</param>
        /// <returns>An ActionResult that indicates the outcome of the delete operation. Returns NoContent if the claim was
        /// successfully deleted; otherwise, returns NotFound if no claim with the specified identifier exists.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            var deleted = await _claimsService.DeleteClaimAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
