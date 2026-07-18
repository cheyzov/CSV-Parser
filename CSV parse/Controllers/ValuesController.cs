using CSV_parse.Dto;
using CSV_parse.Services;
using Microsoft.AspNetCore.Mvc;

namespace CSV_parse.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ValuesController : ControllerBase
{
    private readonly IValuesQueryService _valuesQueryService;

    public ValuesController(IValuesQueryService valuesQueryService)
    {
        _valuesQueryService = valuesQueryService;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<IReadOnlyList<ValueDto>>> GetLatestValues(
        [FromQuery] string fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest("fileName обязателен.");
        }

        var values = await _valuesQueryService.GetLatestValuesAsync(fileName, cancellationToken);
        if (values.Count == 0)
        {
            return NotFound($"Файл '{fileName}' не найден.");
        }

        return Ok(values);
    }
}
