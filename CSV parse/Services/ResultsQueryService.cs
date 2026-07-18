using CSV_parse.Dto;
using CSV_parse.Query;
using CSV_parse.Models;
using Microsoft.EntityFrameworkCore;

namespace CSV_parse.Services;

public sealed class ResultsQueryService : IResultsQueryService
{
    private readonly AppDbContext _context;

    public ResultsQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ResultDto>> GetResultsAsync(ResultsQueryDto query, CancellationToken cancellationToken)
    {
        if (query.FirstOperationDateFrom > query.FirstOperationDateTo)
        {
            throw new ArgumentException("firstOperationDateFrom не может быть больше firstOperationDateTo.");
        }

        if (query.AverageValueFrom > query.AverageValueTo)
        {
            throw new ArgumentException("averageValueFrom не может быть больше averageValueTo.");
        }

        if (query.AverageExecutionTimeFrom > query.AverageExecutionTimeTo)
        {
            throw new ArgumentException("averageExecutionTimeFrom не может быть больше averageExecutionTimeTo.");
        }

        var dbQuery = _context.Results.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.FileName))
        {
            dbQuery = dbQuery.Where(result => result.FileName == query.FileName);
        }

        if (query.FirstOperationDateFrom.HasValue)
        {
            dbQuery = dbQuery.Where(result => result.FirstOperationDate >= NormalizeDate(query.FirstOperationDateFrom.Value));
        }

        if (query.FirstOperationDateTo.HasValue)
        {
            dbQuery = dbQuery.Where(result => result.FirstOperationDate <= NormalizeDate(query.FirstOperationDateTo.Value));
        }

        if (query.AverageValueFrom.HasValue)
        {
            dbQuery = dbQuery.Where(result => result.AverageValue >= query.AverageValueFrom.Value);
        }

        if (query.AverageValueTo.HasValue)
        {
            dbQuery = dbQuery.Where(result => result.AverageValue <= query.AverageValueTo.Value);
        }

        if (query.AverageExecutionTimeFrom.HasValue)
        {
            dbQuery = dbQuery.Where(result => result.AverageExecutionTime >= query.AverageExecutionTimeFrom.Value);
        }

        if (query.AverageExecutionTimeTo.HasValue)
        {
            dbQuery = dbQuery.Where(result => result.AverageExecutionTime <= query.AverageExecutionTimeTo.Value);
        }

        var results = await dbQuery
            .OrderBy(result => result.FileName)
            .ThenBy(result => result.FirstOperationDate)
            .Select(result => new ResultDto(
                result.Id,
                result.FileName,
                result.DateDeltaSeconds,
                result.FirstOperationDate,
                result.AverageExecutionTime,
                result.AverageValue,
                result.MedianValue,
                result.MaxValue,
                result.MinValue))
            .ToListAsync(cancellationToken);

        return results;
    }

    private static DateTime NormalizeDate(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }
}
