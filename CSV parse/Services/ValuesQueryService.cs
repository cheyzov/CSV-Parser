using CSV_parse.Dto;
using CSV_parse.Models;
using Microsoft.EntityFrameworkCore;

namespace CSV_parse.Services;

public sealed class ValuesQueryService : IValuesQueryService
{
    private readonly AppDbContext _context;

    public ValuesQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ValueDto>> GetLatestValuesAsync(string fileName, CancellationToken cancellationToken)
    {
        var normalizedFileName = Path.GetFileName(fileName);

        var values = await _context.Values
            .AsNoTracking()
            .Where(value => value.FileName == normalizedFileName)
            .OrderByDescending(value => value.Date)
            .ThenByDescending(value => value.Id)
            .Take(10)
            .Select(value => new ValueDto(
                value.Id,
                value.FileName,
                value.Date,
                value.ExecutionTime,
                value.IndicatorValue))
            .ToListAsync(cancellationToken);

        return values;
    }
}
