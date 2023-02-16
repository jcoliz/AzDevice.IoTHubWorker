// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using FluentModbus;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// IoT Plug and Play implementation for SonBest SM7820B RS485 Temp/Humidity sensor
/// </summary>
/// <remarks>
/// http://www.sonbest.com/english/products/SM7820B.html
/// 
/// Note that this sensor seems to be temperamental to multiple read/write operations within 5-10
/// seconds of each other. This causes quite a bit of implementation complexity which can be
/// avoided by choosing another sensor.
/// </remarks>
public class SonbestSm7820Model :  IComponentModel
{
    #region Properties

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    public int Address { get; private set; }

    public int ModelCode
    { 
        get
        {
            if (ConfigRegisterCache is not null)
                return ConfigRegisterCache[ModelCodeRegister - FirstConfigRegister];
            else
                return -1;
        }
        private set
        {
            // The data sheet seems to indicate that we can WRITE a new model code.
            // It's not clear what is the use backing that, so we are not implementing
            // this functionality in software.
            throw new NotImplementedException();
        }
    }

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

    public int BaudRate 
    { 
        get
        {
            if (ConfigRegisterCache is not null)
                return ConfigRegisterCache[BaudRateRegister - FirstConfigRegister];
            else
                return -1;
        }
        private set
        {
            // Not implemented, ignored right now
            // Changing the baud rate is a risky operation.
            // This sensor is so temperamental as it is, I really don't want to risk this.            
        }
    }

    public double TemperatureCorrection
    { 
        get
        {
            if (ConfigRegisterCache is not null)
                return (double)ConfigRegisterCache[TemperatureCorrectionRegister - FirstConfigRegister] / 100.0;
            else
                return 0;
        }
        private set
        {
            // Queue up the register change for later when it's a good time
            short newval = (short)(value * 100.0);
            RegisterWriteQueue.Enqueue((TemperatureCorrectionRegister, newval));
        }
    }

    public double HumidityCorrection 
    { 
        get
        {
            if (ConfigRegisterCache is not null)
                return (double)ConfigRegisterCache[HumidityCorrectionRegister - FirstConfigRegister] / 100.0;
            else
                return 0;
        }
        private set
        {
            // Queue up the register change for later when it's a good time
            short newval = (short)(value * 100.0);
            RegisterWriteQueue.Enqueue((HumidityCorrectionRegister, newval));
        }
    }

    public double CurrentTemperature { get; private set; }

    public double CurrentHumidity { get; private set; }

    #endregion

    #region ModBus Registers
    const int FirstDataRegister = 0;
    const int TemperatureRegister = 0;
    const int HumidityRegister = 1;
    const int AfterLastDataRegister = 2;
    
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

    /// <summary>
    /// Which modbus client to use for communication
    /// </summary>
    [JsonIgnore]
    public ModbusRtuClient? ModBusClient { get; set; }

    /// <summary>
    /// Cache of registers from the sensor
    /// </summary>
    /// <remarks>
    /// Allows us to read them all in one operation, and provide them to callers
    /// as-needed
    /// </remarks>
    private short[]? ConfigRegisterCache;

    /// <summary>
    /// Queue of write operations
    /// </summary>
    /// <remarks>
    /// Allows us to control the timing of write operations for when we think will
    /// be most successful, given the temperamental nature of this sensor. Also lets
    /// us retain the desired state until the write operation IS finally done.
    /// </remarks>
    private readonly ConcurrentQueue<(int,short)> RegisterWriteQueue = new ConcurrentQueue<(int,short)>();

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
        // NOTE: I really need to refactor all this. Because the sensor is so sensitive to read/write timing,
        // I should have a single background thread which is independently responsible for ALL interaction
        // with the sensor. Even telemetry is read in there. Then all the customer-facing use of this class
        // will always get cached values.

        if (!UartOK)
            return null;

        // Read input registers
        var inputs = ModBusClient!.ReadHoldingRegisters<Int16>(Address,FirstDataRegister,AfterLastDataRegister-FirstDataRegister).ToArray();

        // Save those as telemetry
        var reading = new Telemetry();
        reading.Temperature = (double)inputs[TemperatureRegister-FirstDataRegister] / 100.0;
        reading.Humidity = (double)inputs[HumidityRegister-FirstDataRegister] / 100.0;

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
                ConfigRegisterCache = ModBusClient!.ReadHoldingRegisters<Int16>(Address, FirstConfigRegister, AfterLastConfigRegister - FirstConfigRegister).ToArray();

                // Process any pending register writes
                if (! RegisterWriteQueue.IsEmpty)
                {
                    // Peek the top item
                    if (RegisterWriteQueue.TryPeek(out var top))
                    {
                        var (register, value) = top;

                        // Don't write the value through to the sensor IF it already has that value!
                        if (ConfigRegisterCache[register-FirstConfigRegister] != value) 
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));

                            ModBusClient.WriteSingleRegister(Address, register, value);
                        }

                        // Note that we don't DEQUEUE it until it's successfully been applied without error
                        RegisterWriteQueue.TryDequeue(out _);
                    }

                    // NOTE that we only do ONE change per run through telemetry
                }
            }
            catch
            {
                // Experimenting with how to communicate that this failed.
                if (ConfigRegisterCache is not null)
                    ConfigRegisterCache[ModelCodeRegister - FirstConfigRegister] = -2;
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