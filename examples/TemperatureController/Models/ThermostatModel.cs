using AzDevice.Models;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ThermostatModel : IComponentModel
{    
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    [JsonPropertyName("maxTempSinceLastReboot")]
    public double MaxTemp { get; set; } = double.MinValue;

    [JsonPropertyName("targetTemperature")]
    public double TargetTemp { get; set; }

    #endregion

    #region Telemetry

    private double Temperature
    {
        get
        {
            var dt = DateTimeOffset.UtcNow;
            return TargetTemp + dt.Hour * 100.0 + dt.Minute + dt.Second / 100.0;            
        }
    }

    #endregion

    #region Commands

    protected Task<object> GetMinMaxReport(string jsonparams)
    {
        if (jsonparams.Length > 0)
        {
            var since = JsonSerializer.Deserialize<DateTimeOffset>(jsonparams);
            _minMaxReport.StartTime = since;

        }
        return Task.FromResult<object>(_minMaxReport);
    }

    #endregion

    #region Fields

    private MinMaxReportModel _minMaxReport = new MinMaxReportModel();

    #endregion

    #region IComponentModel

    [JsonIgnore]
    public string dtmi => "dtmi:com:example:Thermostat;1";

    bool IComponentModel.HasTelemetry => true;

    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        return name switch
        {
            "getMaxMinReport" => GetMinMaxReport(jsonparams),
            _ => throw new NotImplementedException($"Command {name} is not implemented on {dtmi}")
        };
    }
 
    IDictionary<string, object> IComponentModel.GetTelemetry()
    {
        // Take the reading
        var reading = Temperature;

        // Update the minmaxreport
        _minMaxReport.MaxTemp = Math.Max(_minMaxReport.MaxTemp, reading);
        _minMaxReport.MinTemp = Math.Min(_minMaxReport.MinTemp, reading);
        _minMaxReport.EndTime = DateTimeOffset.Now;

        // Obviously not a really good average! 🤣
        _minMaxReport.AverageTemp = (_minMaxReport.MinTemp + _minMaxReport.MaxTemp + reading) / 3;

        // Update maxtemp property
        MaxTemp = Math.Max(MaxTemp, reading);

        // Return the reading as telemetry
        return new Dictionary<string, object>()
        {
            { "temperature", reading }
        };
    }

    object IComponentModel.SetProperty(string key, object value)
    {
        if (key != "targetTemperature")
            throw new NotImplementedException();

        return TargetTemp = (double)(JValue)value;
    }

    object IComponentModel.GetProperties()
    {
        return this as ThermostatModel;
    }    

    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        throw new NotImplementedException();
    }

    #endregion
}