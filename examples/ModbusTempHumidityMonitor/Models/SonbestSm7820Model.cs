// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using AzDevice.Models;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

/// <summary>
/// IoT Plug and Play implementation for SonBest SM7820B RS485 Temp/Humidity sensor
/// </summary>
/// <remarks>
/// http://www.sonbest.com/english/products/SM7820B.html
/// </remarks>
public class SonbestSm7820Model :  IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    /// <summary>
    /// Location on Modbus where this sensor is to be found
    /// </summary>
    public int Address { get; private set; }

    /// <summary>
    /// Code for this particular sensor model
    /// </summary>
    public int ModelCode => 
        UartOK switch
        {
            true => _client!.ReadHoldingRegisters<Int16>(Address, ModelCodeRegister, 1).ToArray()[0],
            false => -1
        };

    /// <summary>
    /// Bus speed currently expected by this device
    /// </summary>
    /// <remarks>
    /// This is an enum. The DTMI knows how to handle it. We do not allow changing it, because that seems dangerous
    /// </remarks>
    public int BaudRate => 
        UartOK switch
        {
            true => _client!.ReadHoldingRegisters<Int16>(Address, BaudRateRegister, 1).ToArray()[0],
            false => -1
        };

    public double TemperatureCorrection
    { 
        get
        {
            return UartOK switch
            {
                true => (double)(_client!.ReadHoldingRegisters<Int16>(Address, TemperatureCorrectionRegister, 1).ToArray()[0]) / 100.0,
                false => 0
            };
        }
        private set
        {
            if (UartOK)
            {
                short newval = (short)(value * 100.0);
                _client.WriteSingleRegister(Address, TemperatureCorrectionRegister, newval);
            }
        }
    }

    public double HumidityCorrection 
    { 
        get
        {
            return UartOK switch
            {
                true => (double)(_client!.ReadHoldingRegisters<Int16>(Address, HumidityCorrectionRegister, 1).ToArray()[0]) / 100.0,
                false => 0
            };
        }
        private set
        {
            if (UartOK)
            {
                short newval = (short)(value * 100.0);
                _client.WriteSingleRegister(Address, HumidityCorrectionRegister, newval);
            }
        }
    }

    public double CurrentTemperature { get; private set; }

    public double CurrentHumidity { get; private set; }

    #endregion

    #region Telemetry

    public class Telemetry
    {
        public double Temperature { get; set; }

        public double Humidity { get; set; }
    }

    #endregion

    #region Commands
    #endregion

    #region Constructor
    public SonbestSm7820Model(IModbusClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }
    #endregion

    #region Log Identity
    /// <summary>
    /// How should this component appear in the logs?
    /// </summary>
    /// <returns>String to identify the current device</returns>
    public override string ToString()
    {
        return $"SM7820@{Address}";
    }
    #endregion

    #region Fields

    /// <summary>
    /// Which modbus client to use for communication
    /// </summary>
    private readonly IModbusClient _client;

    /// <summary>
    /// Where to log events
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Whether we should expect modbus operations to succeed
    /// </summary>
    private bool UartOK => _client.IsConnected && Address > 0;
    #endregion

    #region ModBus Registers
    const int FirstDataRegister = 0;
    const int TemperatureRegister = 0;
    const int HumidityRegister = 1;
    const int AfterLastDataRegister = 2;
    
    const int ModelCodeRegister = 0x64;
    const int BaudRateRegister = 0x67;
    const int TemperatureCorrectionRegister = 0x6B;
    const int HumidityCorrectionRegister = 0x6C;
    #endregion

    #region IComponentModel

    /// <summary>
    /// Identifier for the model implemented here
    /// </summary>
    [JsonIgnore]
    public string dtmi => "dtmi:azdevice:sonbest_sm7820;1";

    /// <summary>
    /// Get an object containing all current telemetry
    /// </summary>
    /// <returns>All telemetry we wish to send at this time, or null for don't send any</returns>
    object? IComponentModel.GetTelemetry()
    {
        if (!UartOK)
            return null;

        // Grab telemetry from sensor
        var inputs = _client!.ReadHoldingRegisters<Int16>(Address,FirstDataRegister,AfterLastDataRegister-FirstDataRegister).ToArray();

        // Save those as telemetry
        var reading = new Telemetry()
        {
            Temperature = (double)inputs[TemperatureRegister - FirstDataRegister] / 100.0,
            Humidity = (double)inputs[HumidityRegister - FirstDataRegister] / 100.0
        };

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
        return this as SonbestSm7820Model;
    }

    /// <summary>
    /// Set the application intitial state from the supplied configuration values
    /// </summary>
    /// <param name="values">Dictionary of all known configuration values which could apply to this component</param>
    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("Address"))
            Address = Convert.ToInt16(values["Address"]);

        _logger.LogDebug(LogEvents.SensorReady, "Sensor {sensor}: Ready? {isready}",this.ToString(),UartOK);
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