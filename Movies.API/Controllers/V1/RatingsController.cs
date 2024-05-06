using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests.V1;
using Movies.Contracts.Responses;

namespace Movies.API.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut(ApiEndpoints.V1.Movies.Rate)]
    public async Task<IActionResult> RateMovie([FromRoute] Guid id,
        [FromBody] RateMovieRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var result = await _ratingService.RateMovieAsync(id, request.Rating, userId.Value, token);
        return result ? Ok() : NotFound();
    }
    
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete(ApiEndpoints.V1.Movies.DeleteRating)]
    public async Task<IActionResult> DeleteRating([FromRoute] Guid id,
        CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var result = await _ratingService.DeleteRatingAsync(id, userId.Value, token);
        return result ? Ok() : NotFound();
    }
    
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<MovieRatingResponse>), StatusCodes.Status200OK)]
    [HttpGet(ApiEndpoints.V1.Ratings.GetUserRatings)]
    public async Task<IActionResult> GetUserRatings(CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var ratings = await _ratingService.GetRatingsForUserAsync(userId.Value, token);
        var ratingsResponse = ratings.MapToResponse();
        return Ok(ratingsResponse);
    }
}