using System.Text.Json.Serialization;
using AzDevice.Models;

public class ThermostatModel : IComponentModel
{
    #region Properties

    [JsonPropertyName("maxTempSinceLastReboot")]
    public double MaxTemp { get; } = 1234.5;

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
        throw new NotImplementedException();
    }

    object IComponentModel.GetProperties()
    {
        return this as ThermostatModel;
    }    
    #endregion
}