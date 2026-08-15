using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Web.Controllers.Api;

/// <summary>
/// Read-only REST endpoints over the community media catalogue, for external consumers (§25).
/// Public - the same content anonymous visitors can already browse on <c>/Search</c> and
/// <c>/Media/Details/{id}</c>.
/// <para>
/// Layering: <c>ApiController → IMediaService → IUnitOfWork → Repository → EF Core → SQL Server</c>.
/// This controller never touches <c>PersonaNestDbContext</c> and never returns an EF entity -
/// every response is one of the same DTOs the MVC views already use.
/// </para>
/// </summary>
[ApiController]
[Route("api/media")]
[AllowAnonymous]
[Produces("application/json")]
public class MediaApiController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaApiController(IMediaService mediaService)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
    }

    /// <summary>Searches the media catalogue by title/creator and category.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MediaCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MediaCardDto>>> Search(
        [FromQuery] MediaSearchRequest request, CancellationToken cancellationToken)
        => Ok(await _mediaService.SearchAsync(request, cancellationToken));

    /// <summary>A single media item's full detail, with cached rating/entry-count aggregates.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MediaDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaDetailDto>> GetById(
        int id, CancellationToken cancellationToken)
    {
        // viewerId: null - an external API consumer has no PersonaNest session, so viewer-only
        // fields (e.g. "is this in my collection") are always their anonymous defaults.
        var media = await _mediaService.GetDetailsAsync(id, viewerId: null, cancellationToken);
        return media is null ? NotFound() : Ok(media);
    }
}
