using AzDevice.Models;

public class ControllerModel : IRootModel
{
    public TimeSpan TelemetryPeriod => TimeSpan.FromSeconds(10);

    public DeviceInformationModel DeviceInfo => Components["deviceInformation"] as DeviceInformationModel ?? throw new NotImplementedException();

    public IDictionary<string, IComponentModel> Components { get; } = new Dictionary<string, IComponentModel>()
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

    public string dtmi => "dtmi:com:example:TemperatureController;2";

    public bool HasTelemetry => false;

    public Task<object> DoCommandAsync(string name, byte[] data)
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, object> GetTelemetry()
    {
        throw new NotImplementedException();
    }

    public Task<string> LoadConfigAsync() => Task.FromResult<string>("No config needed");

    public object SetProperty(string key, object value)
    {
        throw new NotImplementedException();
    }

    public object GetProperties()
    {
        throw new NotImplementedException();
    }
}