using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Web.Controllers.Api;

/// <summary>Read-only REST endpoint over the 7 media categories, for external consumers (§25).</summary>
[ApiController]
[Route("api/categories")]
[AllowAnonymous]
[Produces("application/json")]
public class CategoriesApiController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public CategoriesApiController(IMediaService mediaService)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await _mediaService.GetCategoriesAsync(cancellationToken));
}
