// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using AzDevice.Models;
using System.Text.Json.Serialization;

public class Xymd02Model :  IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    /// <summary>
    /// Location on Modbus where this sensor is to be found
    /// </summary>
    public int Address { get; private set; }

    public int BaudRate
    {
        get
        {
            return UartOK ? _client.ReadHoldingRegisters<Int16>(Address, BaudRateRegister, 1)[0] : 0;
        }
    }

    public double TemperatureCorrection
    {
        get
        {
            return UartOK ? _client.ReadHoldingRegisters<Int16>(Address,TemperatureCorrectionRegister,1)[0] / 10.0 : 0;
        }
        set
        {
            if (UartOK)
                _client.WriteSingleRegister(Address,TemperatureCorrectionRegister,(short)(value*10.0));
        }
    }

    public double HumidityCorrection
    {
        get
        {
            return UartOK ? _client.ReadHoldingRegisters<Int16>(Address,HumidityCorrectionRegister,1)[0] / 10.0 : 0;
        }
        set
        {
            if (UartOK)
                _client.WriteSingleRegister(Address,HumidityCorrectionRegister,(short)(value*10.0));
        }
    }

    public double CurrentTemperature { get; private set; }

    public double CurrentHumidity { get; private set; }

    #endregion

    #region ModBus Registers
    const int TemperatureRegister = 1;
    const int HumidityRegister = 2;
    const int AddressRegister = 0x101;
    const int BaudRateRegister = 0x102;
    const int TemperatureCorrectionRegister = 0x103;
    const int HumidityCorrectionRegister = 0x104;
    #endregion

    #region Telemetry

    public class Telemetry
    {
        public double Temperature { get; set; }

        public double Humidity { get; set; }
    }

    #endregion

    #region Commands
    private void SetAddress(int address)
    {
        _client.WriteSingleRegister(Address,AddressRegister,(short)address);
    }
    #endregion

    #region Constructor
    public Xymd02Model(IModbusClient client, ILogger logger)
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
        return $"XY-MD02@{Address}";
    }
    #endregion

    #region Internals
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
        if (!UartOK)
            return null;

        // Read input registers
        var inputs = _client.ReadInputRegisters<Int16>(Address,TemperatureRegister,2).ToArray();

        // Save those as telemetry
        var reading = new Telemetry();
        reading.Temperature = (double)inputs[0] / 10.0;
        reading.Humidity = (double)inputs[1] / 10.0;

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