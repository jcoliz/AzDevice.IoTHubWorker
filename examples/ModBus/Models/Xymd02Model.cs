// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Xymd02Model :  IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    public int Address { get; set; }

    public double CurrentTemperature { get; private set; }

    public double CurrentHumidity { get; private set; }

    public double TemperatureCorrection { get; set; }

    public double HumidityCorrection { get; set; }

    public int BaudRate { get; set; }

    #endregion

    #region Telemetry

    /// <summary>
    /// Generates simulated telemetry in case we don't have an actual sensor
    /// attached.
    /// </summary>
    /// <remarks>
    /// Whether or not we are running with a real sensor is a runtime decision
    /// based on "Initial State" configuration.
    /// </remarks>
    public class SimulatedTelemetry
    {
        public SimulatedTelemetry()
        {
            var dt = DateTimeOffset.UtcNow;
            Temperature = dt.Hour * 100.0 + dt.Minute + dt.Second / 100.0;            
            Humidity = (dt.Hour * 100.0 + dt.Minute + dt.Second / 100.0) / 2400.0;
        }

        public double Temperature { get; private set; }

        public double Humidity { get; private set; }
    }

    #endregion

    #region Commands
    #endregion

    #region Log Identity
    /// <summary>
    /// How should this component appear in the logs?
    /// </summary>
    /// <returns>String to identify the current device</returns>
    public override string ToString()
    {
        return "XY-MD02";
    }
    #endregion

    #region Fields
    #endregion

    #region IComponentModel

    /// <summary>
    /// Identifier for the model implemented here
    /// </summary>
    [JsonIgnore]
    public string dtmi => "dtmi:azdevice:xy_md02;1";

    /// <summary>
    /// Get an object containing all current telemetry
    /// </summary>
    /// <returns>All telemetry we wish to send at this time, or null for don't send any</returns>
    object? IComponentModel.GetTelemetry()
    {
        // Take the reading
        var reading = new SimulatedTelemetry();

        // Update the properties which track the current values
        CurrentHumidity = reading.Humidity;
        CurrentTemperature = reading.Temperature;

        // Return it
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
        if (key == "TemperatureCorrection")
            return TemperatureCorrection = Convert.ToDouble(jsonvalue);

        if (key == "HumidityCorrection")
            return HumidityCorrection = Convert.ToDouble(jsonvalue);

        throw new NotImplementedException($"Property {key} is not implemented on {dtmi}");
    }

    /// <summary>
    /// Get an object containing all properties known to this model
    /// </summary>
    /// <returns>All known properties, and their current state</returns>
    object IComponentModel.GetProperties()
    {
        return this as Xymd02Model;
    }

    /// <summary>
    /// Set the application intitial state from the supplied configuration values
    /// </summary>
    /// <param name="values">Dictionary of all known configuration values which could apply to this component</param>
    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("Address"))
            TemperatureCorrection = Convert.ToInt16(values["Address"]);
    }

    /// <summary>
    /// Execute the given command
    /// </summary>
    /// <param name="name">Name of the command</param>
    /// <param name="jsonparams">Parameters for the command (will be deserialized from JSON)</param>
    /// <returns>Unserialized result of the action, or new() for empty result</returns>
    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        throw new NotImplementedException($"Command {name} is not implemented on {dtmi}");
    }
 
    #endregion    
}