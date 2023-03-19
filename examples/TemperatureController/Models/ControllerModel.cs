// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using AzDevice.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

/// <summary>
/// Implementation for IoT Plug-and-play example temperature controller
/// </summary>
/// <remarks>
/// "dtmi:com:example:TemperatureController;2";
/// </remarks>
public class ControllerModel : IRootModel
{
    #region Properties

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; private set; } = "Unassigned";

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

    public class Telemetry
    {
        [JsonPropertyName("workingSet")]
        public double WorkingSetKiB
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

    protected Task<object> Reboot(string jsonparams)
    {
        var delay = (jsonparams.Length > 0) ? JsonSerializer.Deserialize<int>(jsonparams) : 0;

        // TODO: Do something with this command

        return Task.FromResult<object>(new());
    }

    #endregion

    #region Log Identity
    /// <summary>
    /// How should this model appear in the logs?
    /// </summary>
    /// <returns>String to identify the current model</returns>
    public override string ToString()
    {
        return $"{DeviceInformation.Manufacturer} {DeviceInformation.DeviceModel} S/N:{SerialNumber} ver:{DeviceInformation.SoftwareVersion}";
    }
    #endregion

    #region IRootModel

    /// <summary>
    /// How often to send telemetry, or zero to avoid sending any telemetry right now
    /// </summary>
    TimeSpan IRootModel.TelemetryPeriod => _TelemetryPeriod;

    /// <summary>
    /// The components which are contained within this one
    /// </summary>
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

    /// <summary>
    /// Identifier for this model
    /// </summary>
    [JsonIgnore]
    public string dtmi => "dtmi:com:example:TemperatureController;2";

    /// <summary>
    /// Get an object containing all current telemetry
    /// </summary>
    /// <returns>All telemetry we wish to send at this time, or null for don't send any</returns>
    object? IComponentModel.GetTelemetry()
    {
        // Take the reading, return it
        return new Telemetry();
    }

    /// <summary>
    /// Set a particular property to the given value
    /// </summary>
    /// <param name="key">Which property</param>
    /// <param name="jsonvalue">Value to set (will be deserialized from JSON)</param>
    /// <returns>The unserialized new value of the property</returns>
    object IComponentModel.SetProperty(string key, string jsonvalue)
    {
        if (key != "telemetryPeriod")
            throw new NotImplementedException($"Property {key} is not implemented on {dtmi}");

        return TelemetryPeriod = System.Text.Json.JsonSerializer.Deserialize<string>(jsonvalue)!;
    }

    /// <summary>
    /// Get an object containing all properties known to this model
    /// </summary>
    /// <returns>All known properties, and their current state</returns>
    object IComponentModel.GetProperties()
    {
        return this as ControllerModel;
    }

    /// <summary>
    /// Set the application intitial state from the supplied configuration values
    /// </summary>
    /// <param name="values">Dictionary of all known configuration values which could apply to this component</param>
    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("Version"))
            DeviceInformation.SoftwareVersion = values["Version"];

        if (values.ContainsKey("serialNumber"))
            SerialNumber = values["serialNumber"];

        if (values.ContainsKey("telemetryPeriod"))
            TelemetryPeriod = values["telemetryPeriod"];
    }

    /// <summary>
    /// Execute the given command
    /// </summary>
    /// <param name="name">Name of the command</param>
    /// <param name="jsonparams">Parameters for the command (will be deserialized from JSON)</param>
    /// <returns>Unserialized result of the action, or new() for empty result</returns>
    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        return name switch
        {
            "reboot" => Reboot(jsonparams),
            _ => throw new NotImplementedException($"Command {name} is not implemented on {dtmi}")
        };
    }

    #endregion    
}