using CSV_parse.Dto;
using CSV_parse.Query;
using CSV_parse.Services;
using Microsoft.AspNetCore.Mvc;

namespace CSV_parse.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ResultsController : ControllerBase
{
    private readonly IResultsQueryService _resultsQueryService;

    public ResultsController(IResultsQueryService resultsQueryService)
    {
        _resultsQueryService = resultsQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResultDto>>> GetResults(
        [FromQuery] ResultsQueryDto query,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await _resultsQueryService.GetResultsAsync(query, cancellationToken);
            return Ok(results);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
