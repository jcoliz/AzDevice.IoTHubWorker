using AzDevice.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ControllerModel : IRootModel
{
    #region Properties

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; } = "1234567890";

    #endregion

    #region IRootModel

    TimeSpan IRootModel.TelemetryPeriod => TimeSpan.FromSeconds(10);

    DeviceInformationModel IRootModel.DeviceInfo => ((IRootModel)this).Components["deviceInformation"] as DeviceInformationModel ?? throw new NotImplementedException();

    IDictionary<string, IComponentModel> IRootModel.Components { get; } = new Dictionary<string, IComponentModel>()
    {
        { 
            "deviceInformation", 
            new DeviceInformationModel()
            {
                Manufacturer = "Example",
                DeviceModel = "TemperatureController",
                SoftwareVersion = "0.0.1"
            }
        },
        {
            "thermostat1",
            new ThermostatModel() { MaxTemp = 1000.0 }
        },
        {
            "thermostat2",
            new ThermostatModel() { MaxTemp = 2000.0 }
        },
    };

    #endregion

    #region IComponentModel

    string IComponentModel.dtmi => "dtmi:com:example:TemperatureController;2";

    bool IComponentModel.HasTelemetry => false;

    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        if (name != "reboot")
            throw new NotImplementedException();

        var delay = (jsonparams.Length > 0) ? JsonSerializer.Deserialize<int>(jsonparams) : 0;

        // TODO: Do something with this command

        return Task.FromResult<object>(new());
    }

    IDictionary<string, object> IComponentModel.GetTelemetry()
    {
        throw new NotImplementedException();
    }

    Task<string> IRootModel.LoadConfigAsync() => Task.FromResult<string>("No config needed");

    object IComponentModel.SetProperty(string key, object value)
    {
        throw new NotImplementedException();
    }

    object IComponentModel.GetProperties()
    {
        return this as ControllerModel;
    }
    #endregion
}