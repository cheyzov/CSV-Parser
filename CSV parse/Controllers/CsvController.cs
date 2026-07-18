using CSV_parse.Response;
using CSV_parse.Services;
using Microsoft.AspNetCore.Mvc;

namespace CSV_parse.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CsvController : ControllerBase
{
    private readonly ICsvImportService _csvImportService;

    public CsvController(ICsvImportService csvImportService)
    {
        _csvImportService = csvImportService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CsvUploadResponseDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _csvImportService.ImportAsync(file, cancellationToken);
            return Ok(response);
        }
        catch (CsvValidationException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
