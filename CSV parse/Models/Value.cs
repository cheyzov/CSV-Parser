namespace CSV_parse.Models;

public class Value
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public double ExecutionTime { get; set; }

    public double IndicatorValue { get; set; }
}
