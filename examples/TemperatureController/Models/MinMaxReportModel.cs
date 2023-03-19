// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using System.Text.Json.Serialization;

/// <summary>
/// Report showing statistics about temperatures over time
/// </summary>
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
