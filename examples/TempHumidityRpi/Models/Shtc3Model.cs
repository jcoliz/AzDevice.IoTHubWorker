// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Shtc3Model :  IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    [JsonPropertyName("Id")]
    public int Id => PhysicalSensor?.Id ?? Int32.MaxValue;

    [JsonPropertyName("CurrentTemperature")]
    public double CurrentTemperature { get; private set; }

    [JsonPropertyName("CurrentHumidity")]
    public double CurrentHumidity { get; private set; }

    [JsonPropertyName("TemperatureCorrection")]
    public double TemperatureCorrection { get; set; }

    [JsonPropertyName("HumidityCorrection")]
    public double HumidityCorrection { get; set; }

    #endregion

    #region Telemetry

    public class Telemetry
    {
        public Telemetry()
        {
            var dt = DateTimeOffset.UtcNow;
            Temperature = dt.Hour * 100.0 + dt.Minute + dt.Second / 100.0;            
            Humidity = (dt.Hour * 100.0 + dt.Minute + dt.Second / 100.0) / 2400.0;
        }

        [JsonPropertyName("Temperature")]
        public double Temperature { get; private set; }

        [JsonPropertyName("Humidity")]
        public double Humidity { get; private set; }
    }

    #endregion

    #region Commands
    #endregion

    #region Fields
    /// <summary>
    /// Connection to physical sensors
    /// </summary>
    /// <remarks>
    /// Or (null), indicating we are sending simulated sensor data
    /// </remarks>
    private Shtc3Physical? PhysicalSensor = null;
    #endregion

    #region IComponentModel

    /// <summary>
    /// Identifier for the model implemented here
    /// </summary>
    [JsonIgnore]
    public string dtmi => "dtmi:azdevice:shtc3;1";

    /// <summary>
    /// Get an object containing all current telemetry
    /// </summary>
    /// <returns>All telemetry we wish to send at this time, or null for don't send any</returns>
    object? IComponentModel.GetTelemetry()
    {
        // If we have a physical sensor connected, use that
        if (PhysicalSensor is not null)
        {
            if (PhysicalSensor.TryUpdate())
            {
                // Update the properties which track the current values
                CurrentHumidity = PhysicalSensor.Humidity;
                CurrentTemperature = PhysicalSensor.Temperature;

                // Return it
                return PhysicalSensor;
            }
            else
                return null;

        }
        // Otherwise, use simulated telemetry
        else
        {
            // Take the reading
            var reading = new Telemetry();

            // Update the properties which track the current values
            CurrentHumidity = reading.Humidity;
            CurrentTemperature = reading.Temperature;

            // Return it
            return reading;
        }
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
        return this as Shtc3Model;
    }

    /// <summary>
    /// Set the application intitial state from the supplied configuration values
    /// </summary>
    /// <param name="values">Dictionary of all known configuration values which could apply to this component</param>
    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("TemperatureCorrection"))
            TemperatureCorrection = Convert.ToDouble(values["TemperatureCorrection"]);

        if (values.ContainsKey("HumidityCorrection"))
            HumidityCorrection = Convert.ToDouble(values["HumidityCorrection"]);

        if (values.ContainsKey("Physical"))
        {
            var usephysical = Convert.ToBoolean(values["Physical"]);
            if (usephysical)
            {
                PhysicalSensor = new Shtc3Physical();
            }
        }
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