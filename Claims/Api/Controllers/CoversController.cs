using Claims.Application.Covers;
using Claims.Domain.Covers;
using Claims.Infrastructure.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController : ControllerBase
{
    private readonly ICoverService _coverService;
    private readonly ILogger<CoversController> _logger;

    public CoversController(ICoverService coverService, AuditContext auditContext, ILogger<CoversController> logger)
    {
        _coverService = coverService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the insurance premium for the specified coverage type and date range.
    /// </summary>
    /// <remarks>Ensure that <paramref name="startDate"/> is before <paramref name="endDate"/> to avoid
    /// validation errors. This method is asynchronous and should be awaited.</remarks>
    /// <param name="startDate">The start date of the coverage period. Must be earlier than <paramref name="endDate"/>.</param>
    /// <param name="endDate">The end date of the coverage period. Must be later than <paramref name="startDate"/>.</param>
    /// <param name="coverType">The type of coverage for which the premium is calculated. Determines the pricing model applied.</param>
    /// <returns>An <see cref="ActionResult"/> containing the calculated premium amount. Returns an error response if the input
    /// dates are invalid.</returns>
    [HttpPost("compute")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(decimal))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public ActionResult ComputePremiumAsync(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return Ok(PremiumCalculator.ComputePremium(startDate, endDate, coverType));
    }

    /// <summary>
    /// Retrieves all available cover items asynchronously.
    /// </summary>
    /// <remarks>This method issues an HTTP GET request to obtain the complete list of covers. The response is
    /// suitable for use in RESTful APIs and follows standard HTTP response conventions.</remarks>
    /// <returns>An <see cref="ActionResult{T}"/> containing a collection of <see cref="Cover"/> objects. Returns an empty
    /// collection if no covers are found.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Cover>))]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync()
    {
        var results = await _coverService.GetAllCoversAsync();
        return Ok(results);
    }

    /// <summary>
    /// Asynchronously retrieves the cover details associated with the specified identifier.
    /// </summary>
    /// <remarks>This method asynchronously fetches the cover information from the service. Ensure that the
    /// provided identifier corresponds to an existing cover.</remarks>
    /// <param name="id">The unique identifier of the cover to retrieve. This parameter cannot be null or empty.</param>
    /// <returns>An ActionResult containing the cover details if found; otherwise, a not found response.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Cover))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<Cover>> GetAsync(string id)
    {
        var results = await _coverService.GetCoverByIdAsync(id);

        if (results == null)
            return NotFound();

        return Ok(results);
    }

    /// <summary>
    /// Creates a new cover and adds it to the system.
    /// </summary>
    /// <remarks>This method handles HTTP POST requests to add a new cover. The created cover is returned in
    /// the response body with a 200 OK status code.</remarks>
    /// <param name="cover">The cover to create. This parameter must not be null.</param>
    /// <returns>An ActionResult that contains the created cover.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Cover))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<ActionResult> CreateAsync(Cover cover)
    {
        await _coverService.CreateCoverAsync(cover);
        return Ok(cover);
    }

    /// <summary>
    /// Deletes the cover with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the cover to delete. Cannot be null or empty.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing <see langword="true"/> if the cover was successfully deleted;
    /// otherwise, <see langword="false"/>. Returns a NotFound result if the cover does not exist.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> DeleteAsync(string id)
    {
        var deleted = await _coverService.DeleteCoverAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();

    }
 }
