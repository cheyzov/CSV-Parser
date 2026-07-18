using CSV_parse.Dto;

namespace CSV_parse.Services;

public interface IValuesQueryService
{
    Task<IReadOnlyList<ValueDto>> GetLatestValuesAsync(string fileName, CancellationToken cancellationToken);
}
