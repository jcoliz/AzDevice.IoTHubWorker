// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using System.Text.Json.Serialization;

/// <summary>
/// Represents a single point in lat/long space
/// </summary>
public class GeoPointModel
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("long")]
    public double Longitude { get; set; }
}