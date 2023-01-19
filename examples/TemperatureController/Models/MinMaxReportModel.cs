using System.Text.Json.Serialization;

public class MinMaxReportModel
{
    [JsonPropertyName("maxTemp")]
    public double MaxTemp { get; set; }
    [JsonPropertyName("minTemp")]
    public double MinTemp { get; set; }
    [JsonPropertyName("avgTemp")]
    public double AverageTemp { get; set; }
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }
    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; }
}