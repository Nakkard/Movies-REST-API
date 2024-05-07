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
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.V1.Movies.Create)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMovieRequest request, CancellationToken token)
    {
        var movie = request.MapToMovie();
        
        await _movieService.CreateAsync(movie, token);

        var response = movie.MapToResponse();
        
        return CreatedAtAction(nameof(Get), new { idOrSlug = response.Id }, response);
    }
    
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 30, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]
    [HttpGet(ApiEndpoints.V1.Movies.Get)]
    public async Task<IActionResult> Get([FromRoute] string idOrSlug, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        
        var movie = Guid.TryParse(idOrSlug, out var id)
            ? await _movieService.GetByIdAsync(id, userId, token)
            : await _movieService.GetBySlugAsync(idOrSlug, userId, token);

        if (movie is null)
        {
            return NotFound();
        }

        var response = movie.MapToResponse();
        
        return Ok(response);
    }
    
    [ProducesResponseType(typeof(MoviesResponse), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, VaryByQueryKeys = new[]{"title", "year", "sortBy", "pageSize", "page"}, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]
    [HttpGet(ApiEndpoints.V1.Movies.GetAll)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        
        var options = request.MapToOptions()
            .WithUser(userId);
        
        var movies = await _movieService.GetAllAsync(options, token);
        
        var movieCount = await _movieService.GetCountAsync(options.Title, options.YearOfRelease, token);
        
        var moviesResponse = movies.MapToResponse(request.Page, request.PageSize, movieCount);
        
        return Ok(moviesResponse);
    }
    
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    [HttpPut(ApiEndpoints.V1.Movies.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id,
        [FromBody] UpdateMovieRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        
        var movie = request.MapToMovie(id);

        var updatedMovie = await _movieService.UpdateAsync(movie, userId, token);
        
        if (updatedMovie is null)
        {
            return NotFound();
        }
        
        var response = updatedMovie.MapToResponse();
        return Ok(response);
    }
    
    [Authorize(AuthConstants.AdminUserPolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete(ApiEndpoints.V1.Movies.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
    {
        var deleted = await _movieService.DeleteByIdAsync(id, token);

        if (!deleted)
        {
            return NotFound();
        }
        
        return Ok();
    }
}