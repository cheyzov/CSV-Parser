namespace CSV_parse.Dto;

public sealed record ValueDto(

    int Id,

    string FileName,

    DateTime Date,

    double ExecutionTime,

    double IndicatorValue
    
    );
