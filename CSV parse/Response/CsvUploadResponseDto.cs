using CSV_parse.Dto;

namespace CSV_parse.Response;

public sealed record CsvUploadResponseDto(

    string FileName,

    int RowsCount,

    ResultDto Result
    
    );
