using AzDevice.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MonitorModel : IRootModel
{
    #region Properties

    [JsonPropertyName("SoftwareVersion")]
    public string? SoftwareVersion { get; private set; }

    // Note that the model definition uses "geopoint". 
    // This is not included as a standard schema, see:
    // https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#geospatial-schemas
    //
    // The spec does allog for "point", however, 
    // "NOTE: Because GeoJSON is array-based (coordinates are stored in an 
    // array) and DTDL v2 does not support arrays in Properties, geospatial 
    // types cannot be used in Property schemas, but can be used in Telemetry 
    // and Commands schemas."
    [JsonPropertyName("Location")]
    public string? Location { get; private set; } = "Unassigned";

    [JsonPropertyName("City")]
    public string? City { get; private set; } = "City";

    [JsonPropertyName("Country")]
    public string? Country { get; private set; } = "Country";

    [JsonPropertyName("HeartbeatUTC")]
    public DateTimeOffset HeartbeatUTC => DateTimeOffset.UtcNow;

    [JsonPropertyName("LedBrightness")]
    public int LedBrightness { get; set; }

    #endregion

    #region Telemetry

    private int Temperature => 1;

    private int Pressure => 2;

    private int Humidity => 3;

    private double Lattitude => 1.0;

    private double Longitude => 2.0;

    private double AirQuality => 3.0;

    private double CarbonMonoxide => 4.0;

    private double NitrogenDioxide => 5.0;

    private double Ozone => 6.0;

    private double SulpherDioxide => 7.0;

    private double Ammonia => 8.0;

    private double ParticulateMatter25 => 9.0;

    private double ParticulateMatter10 => 10.0;

    private double WindSpeed => 11.0;

    private int WindDirection => 12;

    private double MemoryUsageKiB
    {
        get
        {
            var ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            // Convert to Kibibits
            return (double)ws / (1024.0/8.0);
        }
    }

    #endregion

    #region Commands
    #endregion

    #region Log Identity
    public override string ToString()
    {
        return $"Climate Monitor {SoftwareVersion} {dtmi}";
    }
    #endregion

    #region IRootModel

    TimeSpan IRootModel.TelemetryPeriod => TimeSpan.FromSeconds(10);

    IDictionary<string, IComponentModel> IRootModel.Components { get; } = new Dictionary<string, IComponentModel>();

    #endregion

    #region IComponentModel

    [JsonIgnore]
    public string dtmi => "dtmi:com:example:climatemonitor;1";

    bool IComponentModel.HasTelemetry => true;

    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        throw new NotImplementedException($"Command {name} is not implemented on {dtmi}");
    }

    IDictionary<string, object> IComponentModel.GetTelemetry()
    {
        // Return the reading as telemetry
        return new Dictionary<string, object>()
        {
            { "memoryUsage", MemoryUsageKiB }
        };
    }

    object IComponentModel.SetProperty(string key, string jsonvalue)
    {
        if (key == "LedBrightness")
            return LedBrightness = Convert.ToInt32(jsonvalue);

        throw new NotImplementedException($"Property {key} is not implemented on {dtmi}");
    }

    object IComponentModel.GetProperties()
    {
        return this as MonitorModel;
    }

    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("Version"))
            SoftwareVersion = values["Version"];

        if (values.ContainsKey("Location"))
            Location = values["Location"];

        if (values.ContainsKey("City"))
            City = values["City"];

        if (values.ContainsKey("Country"))
            Country = values["Country"];

        if (values.ContainsKey("LedBrightness"))
            LedBrightness = Convert.ToInt32(values["LedBrightness"]);
    }

    #endregion
}