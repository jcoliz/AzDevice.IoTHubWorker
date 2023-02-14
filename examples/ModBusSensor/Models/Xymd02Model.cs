// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using FluentModbus;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Xymd02Model :  IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    public int Address { get; private set; }

    public int BaudRate
    {
        get
        {
            return UartOK ? ModBusClient!.ReadHoldingRegisters<Int16>(Address, BaudRateRegister, 1)[0] : 0;
        }
        set
        {
            if (UartOK)
                ModBusClient!.WriteSingleRegister(Address,BaudRateRegister,(short)value);
        }
    }

    public double TemperatureCorrection
    {
        get
        {
            return UartOK ? ModBusClient!.ReadHoldingRegisters<Int16>(Address,TemperatureCorrectionRegister,1)[0] / 10.0 : 0;
        }
        set
        {
            if (UartOK)
                ModBusClient!.WriteSingleRegister(Address,TemperatureCorrectionRegister,(short)(value*10.0));
        }
    }

    public double HumidityCorrection
    {
        get
        {
            return UartOK ? ModBusClient!.ReadHoldingRegisters<Int16>(Address,HumidityCorrectionRegister,1)[0] / 10.0 : 0;
        }
        set
        {
            if (UartOK)
                ModBusClient!.WriteSingleRegister(Address,HumidityCorrectionRegister,(short)(value*10.0));
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
        ModBusClient!.WriteSingleRegister(Address,AddressRegister,(short)address);
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
    [JsonIgnore]
    public ModbusRtuClient? ModBusClient { get; set; }

    private bool UartOK => ModBusClient is not null && Address > 0;
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
        if (ModBusClient is null || Address == 0)
            return null;

        // Read input registers
        var inputs = ModBusClient!.ReadInputRegisters<Int16>(Address,TemperatureRegister,2).ToArray();

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
    }

    /// <summary>
    /// Execute the given command
    /// </summary>
    /// <param name="name">Name of the command</param>
    /// <param name="jsonparams">Parameters for the command (will be deserialized from JSON)</param>
    /// <returns>Unserialized result of the action, or new() for empty result</returns>
    Task<object> IComponentModel.DoCommandAsync(string name, string jsonparams)
    {
        if (name == "SetAddress")
        {
            var address = Convert.ToInt16(jsonparams);
            SetAddress(address);

            // TODO: Somehow, I need to force a property update for address, because we need
            // to let the twin know where to look for it next time around.

            return Task.FromResult<object>(new());
        }

        throw new NotImplementedException($"Command {name} is not implemented on {dtmi}");
    }
 
    #endregion    
}