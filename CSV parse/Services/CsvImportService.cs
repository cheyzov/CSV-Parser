using System.Globalization;
using CSV_parse.Dto;
using CSV_parse.Response;
using CSV_parse.Models;
using Microsoft.EntityFrameworkCore;

namespace CSV_parse.Services;

public sealed class CsvImportService : ICsvImportService
{
    private const int MinRowsCount = 1;
    private const int MaxRowsCount = 10_000;
    private static readonly DateTime MinDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd'T'HH-mm-ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH-mm-ss'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mm:ss'Z'"
    ];

    private readonly AppDbContext _context;

    public CsvImportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CsvUploadResponseDto> ImportAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new CsvValidationException("Файл не передан.");
        }

        if (file.Length == 0)
        {
            throw new CsvValidationException("Файл пустой.");
        }

        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new CsvValidationException("Имя файла не задано.");
        }

        var values = await ParseCsvAsync(file, fileName, cancellationToken);
        var result = BuildResult(fileName, values);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _context.Values
                .Where(value => value.FileName == fileName)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.Results
                .Where(resultItem => resultItem.FileName == fileName)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.Values.AddRangeAsync(values, cancellationToken);
            await _context.Results.AddAsync(result, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new CsvUploadResponseDto(fileName, values.Count, MapResult(result));
    }

    private static async Task<List<Value>> ParseCsvAsync(
        IFormFile file,
        string fileName,
        CancellationToken cancellationToken)
    {
        var values = new List<Value>();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        var header = await reader.ReadLineAsync(cancellationToken);
        if (!string.Equals(header?.TrimStart('\uFEFF'), "Date;ExecutionTime;Value", StringComparison.Ordinal))
        {
            throw new CsvValidationException("Некорректный заголовок CSV. Ожидается: Date;ExecutionTime;Value.");
        }

        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new CsvValidationException($"Строка {lineNumber}: пустая строка недопустима.");
            }

            var columns = line.Split(';');
            if (columns.Length != 3 || columns.Any(string.IsNullOrWhiteSpace))
            {
                throw new CsvValidationException($"Строка {lineNumber}: запись должна содержать Date, ExecutionTime и Value.");
            }

            values.Add(new Value
            {
                FileName = fileName,
                Date = ParseDate(columns[0], lineNumber),
                ExecutionTime = ParseNonNegativeDouble(columns[1], "ExecutionTime", lineNumber),
                IndicatorValue = ParseNonNegativeDouble(columns[2], "Value", lineNumber)
            });

            if (values.Count > MaxRowsCount)
            {
                throw new CsvValidationException($"Количество строк не может быть больше {MaxRowsCount}.");
            }
        }

        if (values.Count < MinRowsCount)
        {
            throw new CsvValidationException("Количество строк не может быть меньше 1.");
        }

        return values;
    }

    private static DateTime ParseDate(string rawValue, int lineNumber)
    {
        if (!DateTimeOffset.TryParseExact(
                rawValue.Trim(),
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var dateTimeOffset))
        {
            throw new CsvValidationException($"Строка {lineNumber}: Date должен быть датой в корректном формате.");
        }

        var date = dateTimeOffset.UtcDateTime;
        if (date < MinDate)
        {
            throw new CsvValidationException($"Строка {lineNumber}: Date не может быть раньше 01.01.2000.");
        }

        if (date > DateTime.UtcNow)
        {
            throw new CsvValidationException($"Строка {lineNumber}: Date не может быть позже текущей даты.");
        }

        return date;
    }

    private static double ParseNonNegativeDouble(string rawValue, string fieldName, int lineNumber)
    {
        if (!double.TryParse(
                rawValue.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw new CsvValidationException($"Строка {lineNumber}: {fieldName} должен быть числом.");
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new CsvValidationException($"Строка {lineNumber}: {fieldName} должен быть конечным числом.");
        }

        if (value < 0)
        {
            throw new CsvValidationException($"Строка {lineNumber}: {fieldName} не может быть меньше 0.");
        }

        return value;
    }

    private static Result BuildResult(string fileName, IReadOnlyCollection<Value> values)
    {
        var orderedDates = values.Select(value => value.Date).Order().ToArray();
        var orderedIndicatorValues = values.Select(value => value.IndicatorValue).Order().ToArray();
        var middleIndex = orderedIndicatorValues.Length / 2;

        var median = orderedIndicatorValues.Length % 2 == 0
            ? (orderedIndicatorValues[middleIndex - 1] + orderedIndicatorValues[middleIndex]) / 2
            : orderedIndicatorValues[middleIndex];

        return new Result
        {
            FileName = fileName,
            DateDeltaSeconds = (orderedDates[^1] - orderedDates[0]).TotalSeconds,
            FirstOperationDate = orderedDates[0],
            AverageExecutionTime = values.Average(value => value.ExecutionTime),
            AverageValue = values.Average(value => value.IndicatorValue),
            MedianValue = median,
            MaxValue = orderedIndicatorValues[^1],
            MinValue = orderedIndicatorValues[0]
        };
    }

    private static ResultDto MapResult(Result result)
    {
        return new ResultDto(
            result.Id,
            result.FileName,
            result.DateDeltaSeconds,
            result.FirstOperationDate,
            result.AverageExecutionTime,
            result.AverageValue,
            result.MedianValue,
            result.MaxValue,
            result.MinValue);
    }
}

public sealed class CsvValidationException : Exception
{
    public CsvValidationException(string message)
        : base(message)
    {
    }
}
