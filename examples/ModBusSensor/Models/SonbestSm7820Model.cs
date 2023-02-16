// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using FluentModbus;
using System.Text.Json;
using System.Text.Json.Serialization;

public class SonbestSm7820Model :  IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    public int Address { get; private set; }

    public int ModelCode { get; private set; } = -1;

    public enum BaudRateEnum 
    {
        Invalid = 0,
        Baud2400 = 1,
        Baud4800 = 2,
        Baud9600 = 3,
        Baud19200 = 4,
        Baud38400 = 5,
        Baud115200 = 6
    }

    public int BaudRate { get; private set; } = -1;

    public double TemperatureCorrection { get; private set; } = 0.0;

    public double HumidityCorrection { get; private set; } = 0.0;

    public double CurrentTemperature { get; private set; }

    public double CurrentHumidity { get; private set; }

    #endregion

    #region ModBus Registers
    const int FirstDataRegister = 0;
    const int TemperatureRegister = 0;
    const int HumidityRegister = 1;
    const int FirstConfigRegister = 0x64;
    const int ModelCodeRegister = 0x64;
    const int AddressRegister = 0x66;
    const int BaudRateRegister = 0x67;
    const int TemperatureCorrectionRegister = 0x6B;
    const int HumidityCorrectionRegister = 0x6C;
    const int AfterLastConfigRegister = 0x6D;
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
        return $"SM7820@{Address}";
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
    public string dtmi => "dtmi:azdevice:sonbest_sm7820;1";

    /// <summary>
    /// Get an object containing all current telemetry
    /// </summary>
    /// <returns>All telemetry we wish to send at this time, or null for don't send any</returns>
    object? IComponentModel.GetTelemetry()
    {
        if (!UartOK)
            return null;

        // Read input registers
        var inputs = ModBusClient!.ReadHoldingRegisters<Int16>(Address,TemperatureRegister,2).ToArray();

        // Save those as telemetry
        var reading = new Telemetry();
        reading.Temperature = (double)inputs[0] / 100.0;
        reading.Humidity = (double)inputs[1] / 100.0;

        // Update the properties which track the current values
        CurrentHumidity = reading.Humidity;
        CurrentTemperature = reading.Temperature;

        // This sensor really does NOT like to read registers in rapid succession
        // So we are starting a background task here to wait until we think it's ready
        Task.Run(async () => 
        {
            await Task.Delay(TimeSpan.FromSeconds(10));

            try
            {
                var configs = ModBusClient!.ReadHoldingRegisters<Int16>(Address, FirstConfigRegister, AfterLastConfigRegister - FirstConfigRegister).ToArray();

                ModelCode = configs[ModelCodeRegister - FirstConfigRegister];
                BaudRate = configs[BaudRateRegister - FirstConfigRegister];
                TemperatureCorrection = configs[TemperatureCorrectionRegister - FirstConfigRegister];
                HumidityCorrection = configs[HumidityCorrectionRegister - FirstConfigRegister];

            }
            catch
            {
                // Experimenting with how to communicate that this failed.
                ModelCode = -2;
            }
        });

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

        if (key == "BaudRate")
            return BaudRate = Convert.ToInt16(jsonvalue);

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