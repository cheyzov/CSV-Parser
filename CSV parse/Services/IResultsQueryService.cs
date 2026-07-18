using CSV_parse.Dto;
using CSV_parse.Query;

namespace CSV_parse.Services;

public interface IResultsQueryService
{
    Task<IReadOnlyList<ResultDto>> GetResultsAsync(ResultsQueryDto query, CancellationToken cancellationToken);
}
