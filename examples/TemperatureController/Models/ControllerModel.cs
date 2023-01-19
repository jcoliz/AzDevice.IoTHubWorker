using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzDevice.Models;

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
        }
    };

    #endregion

    #region IComponentModel

    string IComponentModel.dtmi => "dtmi:com:example:TemperatureController;2";

    bool IComponentModel.HasTelemetry => false;

    Task<object> IComponentModel.DoCommandAsync(string name, byte[] data)
    {
        if (name != "reboot")
            throw new NotImplementedException();

        var json = Encoding.UTF8.GetString(data);
        var delay = 0;
        if (json.Length > 0)
            delay = JsonSerializer.Deserialize<int>(json);

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