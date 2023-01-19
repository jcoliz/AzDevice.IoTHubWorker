using AzDevice.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

public class ThermostatModel : IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    [JsonPropertyName("maxTempSinceLastReboot")]
    public double MaxTemp { get; set; } = 1234.5;

    [JsonPropertyName("targetTemperature")]
    public double TargetTemp { get; set; } = 1234.5;

    #endregion

    #region IComponentModel
    string IComponentModel.dtmi => "dtmi:com:example:Thermostat;1";

    bool IComponentModel.HasTelemetry => true;

    Task<object> IComponentModel.DoCommandAsync(string name, byte[] data)
    {
        throw new NotImplementedException();
    }

    IDictionary<string, object> IComponentModel.GetTelemetry()
    {
        var dt = DateTimeOffset.UtcNow;
        return new Dictionary<string, object>()
        {
            { "temperature", dt.Hour * 100.0 + dt.Minute + dt.Second / 100.0 }
        };
    }

    object IComponentModel.SetProperty(string key, object value)
    {
        if (key != "targetTemperature")
            throw new NotImplementedException();

        double desired = (double)(JValue)value;
        TargetTemp = desired;
        return TargetTemp;
    }

    object IComponentModel.GetProperties()
    {
        return this as ThermostatModel;
    }    
    #endregion
}