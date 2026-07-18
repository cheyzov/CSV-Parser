namespace CSV_parse.Dto;

public sealed record ResultDto(

    int Id,

    string FileName,

    double DateDeltaSeconds,

    DateTime FirstOperationDate,

    double AverageExecutionTime,

    double AverageValue,

    double MedianValue,

    double MaxValue,

    double MinValue
    
    );
