using AzDevice.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

public class MonitorModel : IRootModel
{
    #region Properties

    [JsonPropertyName("SoftwareVersion")]
    public string? SoftwareVersion { get; private set; }

    // Note that the model definition uses "geopoint". 
    // This is not included as a standard schema, see:
    // https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#geospatial-schemas
    //
    // Azure IoT Explorer does not have UI support for "geopoint". So, we'll wing it!
    //
    // The spec does allog for "point", however, 
    // "NOTE: Because GeoJSON is array-based (coordinates are stored in an 
    // array) and DTDL v2 does not support arrays in Properties, geospatial 
    // types cannot be used in Property schemas, but can be used in Telemetry 
    // and Commands schemas."
    [JsonPropertyName("Location")]
    public GeoPointModel? Location { get; private set; }

    [JsonPropertyName("City")]
    public string? City { get; private set; } = "City";

    [JsonPropertyName("Country")]
    public string? Country { get; private set; } = "Country";

    [JsonPropertyName("HeartbeatUTC")]
    public DateTimeOffset HeartbeatUTC => DateTimeOffset.UtcNow;

    [JsonPropertyName("StartTimeUTC")]
    public DateTimeOffset StartTimeUTC { get; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("LedBrightness")]
    public int LedBrightness { get; set; }

    // Note that telemetry period is not strictly part of the DTMI. Still,
    // it's nice to be able to set it in config, and send down changes to it
    [JsonPropertyName("telemetryPeriod")]
    public string TelemetryPeriod 
    { 
        get
        {
            return XmlConvert.ToString(_TelemetryPeriod);
        } 
        private set
        {
            _TelemetryPeriod = XmlConvert.ToTimeSpan(value);
        }
    }
    private TimeSpan _TelemetryPeriod = TimeSpan.Zero;

    #endregion

    #region Telemetry

    // NOTE: The Climate Monitor repeats the telemetry readings also as read-only properties.
    // In this implementation, I'm only implementing them as telemetry
    public class Telemetry
    {
        [JsonPropertyName("temperature")]
        public int Temperature => 1;

        [JsonPropertyName("pressure")]
        public int Pressure => 2;

        [JsonPropertyName("humidity")]
        public int Humidity => 3;

        [JsonPropertyName("latitude")]
        public double Latitude => 1.0;

        [JsonPropertyName("longitude")]
        public double Longitude => 2.0;

        [JsonPropertyName("aqi")]
        public double AirQuality => 3.0;

        [JsonPropertyName("co")]
        public double CarbonMonoxide => 4.0;

        [JsonPropertyName("no")]
        public double NitrogenMonoxide => 4.5;

        [JsonPropertyName("no2")]
        public double NitrogenDioxide => 5.0;

        [JsonPropertyName("o3")]
        public double Ozone => 6.0;

        [JsonPropertyName("so2")]
        public double SulpherDioxide => 7.0;

        [JsonPropertyName("nh3")]
        public double Ammonia => 8.0;

        [JsonPropertyName("pm10")]
        public double ParticulateMatter10 => 10.0;

        [JsonPropertyName("pm2_5")]
        public double ParticulateMatter25 => 9.0;

        [JsonPropertyName("windspeed")]
        public double WindSpeed => 11.0;

        [JsonPropertyName("winddirection")]
        public int WindDirection => 12;

        [JsonPropertyName("memoryUsage")]
        public double MemoryUsageKiB
        {
            get
            {
                var ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

                // Convert to Kibibits
                return (double)ws / (1024.0/8.0);
            }
        }
    }

    #endregion

    #region Commands
    // Nothing to see here
    #endregion

    #region Log Identity
    public override string ToString()
    {
        return $"Example Climate Monitor ver:{SoftwareVersion}";
    }
    #endregion

    #region IRootModel

    TimeSpan IRootModel.TelemetryPeriod => _TelemetryPeriod;

    // No subcomponents
    IDictionary<string, IComponentModel> IRootModel.Components { get; } = new Dictionary<string, IComponentModel>();

    #endregion

    #region IComponentModel

    [JsonIgnore]
    public string dtmi => "dtmi:com:example:climatemonitor;1";

    object? IComponentModel.GetTelemetry()
    {
        // Take a reading, return it
        return new Telemetry();
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
            Location = JsonSerializer.Deserialize<GeoPointModel>(values["Location"])!;

        if (values.ContainsKey("City"))
            City = values["City"];

        if (values.ContainsKey("Country"))
            Country = values["Country"];

        if (values.ContainsKey("LedBrightness"))
            LedBrightness = Convert.ToInt32(values["LedBrightness"]);

        if (values.ContainsKey("telemetryPeriod"))
            TelemetryPeriod = values["telemetryPeriod"];
    }

    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        throw new NotImplementedException($"Command {name} is not implemented on {dtmi}");
    }

    #endregion
}