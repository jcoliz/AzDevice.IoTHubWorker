using System.Text.Json.Serialization;

public class GeoPointModel
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("long")]
    public double Longitude { get; set; }
}