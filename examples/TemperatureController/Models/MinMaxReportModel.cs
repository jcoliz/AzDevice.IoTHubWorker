using System.Text.Json.Serialization;

public class MinMaxReportModel
{
    [JsonPropertyName("maxTemp")]
    public double MaxTemp { get; set; } = double.MinValue;
    [JsonPropertyName("minTemp")]
    public double MinTemp { get; set; } = double.MaxValue;
    [JsonPropertyName("avgTemp")]
    public double AverageTemp { get; set; }
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;
    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; } = DateTimeOffset.Now;
}