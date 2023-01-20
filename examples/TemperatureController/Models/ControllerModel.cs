using AzDevice.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ControllerModel : IRootModel
{
    #region Properties

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; private set; } = "Unassigned";

    #endregion

    #region Telemetry

    private double WorkingSetKiB
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

    protected Task<object> Reboot(string jsonparams)
    {
        var delay = (jsonparams.Length > 0) ? JsonSerializer.Deserialize<int>(jsonparams) : 0;

        // TODO: Do something with this command

        return Task.FromResult<object>(new());
    }

    #endregion

    #region Log Identity
    public override string ToString()
    {
        return $"{DeviceInformation.Manufacturer} {DeviceInformation.DeviceModel} S/N:{SerialNumber} ver:{DeviceInformation.SoftwareVersion} {dtmi}";
    }
    #endregion

    #region IRootModel

    TimeSpan IRootModel.TelemetryPeriod => TimeSpan.FromSeconds(10);

    [JsonIgnore]
    public IDictionary<string, IComponentModel> Components { get; } = new Dictionary<string, IComponentModel>()
    {
        { 
            "deviceInformation", 
            new DeviceInformationModel()
        },
        {
            "thermostat1",
            new ThermostatModel()
        },
        {
            "thermostat2",
            new ThermostatModel()
        },
    };
    #endregion

    #region Internals
    private DeviceInformationModel DeviceInformation => (Components["deviceInformation"] as DeviceInformationModel)!;

    #endregion

    #region IComponentModel

    [JsonIgnore]
    public string dtmi => "dtmi:com:example:TemperatureController;2";

    bool IComponentModel.HasTelemetry => true;

    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        return name switch
        {
            "reboot" => Reboot(jsonparams),
            _ => throw new NotImplementedException($"Command {name} is not implemented on {dtmi}")
        };
    }

    IDictionary<string, object> IComponentModel.GetTelemetry()
    {
        // Return the reading as telemetry
        return new Dictionary<string, object>()
        {
            { "workingSet", WorkingSetKiB }
        };
    }

    object IComponentModel.SetProperty(string key, string jsonvalue)
    {
        throw new NotImplementedException();
    }

    object IComponentModel.GetProperties()
    {
        return this as ControllerModel;
    }

    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("Version"))
            DeviceInformation.SoftwareVersion = values["Version"];

        if (values.ContainsKey("serialNumber"))
            SerialNumber = values["serialNumber"];
    }

    #endregion    
}