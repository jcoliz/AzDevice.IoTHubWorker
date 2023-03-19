// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using AzDevice.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Implementation for IoT Plug-and-play example thermostat
/// </summary>
/// <remarks>
/// "dtmi:com:example:Thermostat;2";
/// </remarks>
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

    public class Telemetry
    {
        public Telemetry(double target)
        {
            var dt = DateTimeOffset.UtcNow;
            Temperature = target + dt.Hour * 100.0 + dt.Minute + dt.Second / 100.0;            
        }

        [JsonPropertyName("temperature")]
        public double Temperature { get; private set; }
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

    private readonly MinMaxReportModel _minMaxReport = new MinMaxReportModel();

    #endregion

    #region IComponentModel

    /// <summary>
    /// Identifier for the model implemented here
    /// </summary>
    [JsonIgnore]
    public string dtmi => "dtmi:com:example:Thermostat;1";

    /// <summary>
    /// Get an object containing all current telemetry
    /// </summary>
    /// <returns>All telemetry we wish to send at this time, or null for don't send any</returns>
    object? IComponentModel.GetTelemetry()
    {
        // Take the reading
        var reading = new Telemetry(TargetTemp);

        // Update the minmaxreport
        var temp = reading.Temperature;
        _minMaxReport.MaxTemp = Math.Max(_minMaxReport.MaxTemp, temp);
        _minMaxReport.MinTemp = Math.Min(_minMaxReport.MinTemp, temp);
        _minMaxReport.EndTime = DateTimeOffset.Now;

        // Obviously not a really good average! 🤣
        _minMaxReport.AverageTemp = (_minMaxReport.MinTemp + _minMaxReport.MaxTemp + temp) / 3;

        // Update maxtemp property
        MaxTemp = Math.Max(MaxTemp, temp);

        return reading;
    }

    /// <summary>
    /// Set a particular property to the given value
    /// </summary>
    /// <param name="key">Which property</param>
    /// <param name="jsonvalue">Value to set (will be deserialized from JSON)</param>
    /// <returns>The unserialized new value of the property</returns>
    object IComponentModel.SetProperty(string key, string jsonvalue)
    {
        if (key != "targetTemperature")
            throw new NotImplementedException($"Property {key} is not implemented on {dtmi}");

        return TargetTemp = Convert.ToDouble(jsonvalue);
    }

    /// <summary>
    /// Get an object containing all properties known to this model
    /// </summary>
    /// <returns>All known properties, and their current state</returns>
    object IComponentModel.GetProperties()
    {
        return this as ThermostatModel;
    }

    /// <summary>
    /// Set the application intitial state from the supplied configuration values
    /// </summary>
    /// <param name="values">Dictionary of all known configuration values which could apply to this component</param>
    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("targetTemperature"))
            TargetTemp = Convert.ToDouble(values["targetTemperature"]);
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
            "getMaxMinReport" => GetMinMaxReport(jsonparams),
            _ => throw new NotImplementedException($"Command {name} is not implemented on {dtmi}")
        };
    }
 
    #endregion
}