using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers.Api;

/// <summary>
/// REST endpoint over the signed-in caller's own entries, for external consumers (§25).
/// Authenticated via the same cookie scheme as the rest of the site - an API consumer calls this
/// from a signed-in browser session (e.g. Swagger UI opened in-browser), not a separate token
/// flow. Demonstrates the API enforcing the same authorization as the MVC site: an anonymous
/// caller gets 401, never another user's data.
/// </summary>
[ApiController]
[Route("api/entries")]
[Authorize]
[Produces("application/json")]
public class EntriesApiController : ControllerBase
{
    private readonly IEntryService _entryService;

    public EntriesApiController(IEntryService entryService)
    {
        _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
    }

    /// <summary>The caller's own logged entries - never another user's.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(PagedResult<EntrySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<EntrySummaryDto>>> GetMine(
        [FromQuery] MyEntriesRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        return Ok(await _entryService.GetMineAsync(userId, request, cancellationToken));
    }
}
