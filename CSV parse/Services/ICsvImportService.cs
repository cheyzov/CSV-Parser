using CSV_parse.Response;

namespace CSV_parse.Services;

public interface ICsvImportService
{
    Task<CsvUploadResponseDto> ImportAsync(IFormFile? file, CancellationToken cancellationToken);
}
