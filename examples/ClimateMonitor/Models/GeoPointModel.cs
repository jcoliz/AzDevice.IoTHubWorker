// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

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