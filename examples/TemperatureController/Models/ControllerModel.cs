using AzDevice.Models;

public class ControllerModel : IRootModel
{
    public TimeSpan TelemetryPeriod => throw new NotImplementedException();

    public DeviceInformationModel DeviceInfo => throw new NotImplementedException();

    public IDictionary<string, IComponentModel> Components => throw new NotImplementedException();

    public string dtmi => throw new NotImplementedException();

    public bool HasTelemetry => throw new NotImplementedException();

    public Task<object> DoCommandAsync(string name, byte[] data)
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, object> GetTelemetry()
    {
        throw new NotImplementedException();
    }

    public Task<string> LoadConfigAsync()
    {
        throw new NotImplementedException();
    }

    public object SetProperty(string key, object value)
    {
        throw new NotImplementedException();
    }
}