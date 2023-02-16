// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using System.Collections.Concurrent;
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
                throw new ApplicationException("Properties not fetched from sensor yet");
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
                throw new ApplicationException("Properties not fetched from sensor yet");
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
                throw new ApplicationException("Properties not fetched from sensor yet");
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
                throw new ApplicationException("Properties not fetched from sensor yet");
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
        RegisterWriteQueue.Enqueue((AddressRegister, (short)address));

        // NOTE: The whole workflow of changing modbus sensor address from the cloud 
        // needs more thinking-through.
    }
    #endregion

    #region Constructor
    public SonbestSm7820Model(IModbusClient client)
    {
        _client = client;

        ThreadPool.QueueUserWorkItem<SonbestSm7820Model>(BackgroundWork, this, preferLocal: true);
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
    private readonly IModbusClient _client;

    /// <summary>
    /// Cache of registers from the sensor
    /// </summary>
    /// <remarks>
    /// Allows us to read them all in one operation, and provide them to callers
    /// as-needed
    /// </remarks>
    private short[]? ConfigRegisterCache;

    private short[]? InputRegisterCache;

    /// <summary>
    /// Queue of write operations
    /// </summary>
    /// <remarks>
    /// Allows us to control the timing of write operations for when we think will
    /// be most successful, given the temperamental nature of this sensor. Also lets
    /// us retain the desired state until the write operation IS finally done.
    /// </remarks>
    private readonly ConcurrentQueue<(int,short)> RegisterWriteQueue = new ConcurrentQueue<(int,short)>();

    private bool UartOK => _client.IsConnected && Address > 0;
    #endregion

    #region Background Work
    /// <summary>
    /// Runs all work with modbus in the backgound
    /// </summary>
    /// <remarks>
    /// Necessary for this sensor to control timing
    /// </remarks>
    /// <param name="state"></param>
    static void BackgroundWork(SonbestSm7820Model state)
    {
        while(true)
        {
            try
            {
                if (state.UartOK)
                {
                    // First order of business is to read the holding registers, if we don't already have them
                    if(state.ConfigRegisterCache is null)
                    {
                        try
                        {
                            state.ConfigRegisterCache = state._client.ReadHoldingRegisters<Int16>(state.Address, FirstConfigRegister, AfterLastConfigRegister - FirstConfigRegister).ToArray();
                        }
                        catch
                        {
                            state.ConfigRegisterCache = null;
                        }
                    }
                    // Next, process one holding register write
                    else if (! state.RegisterWriteQueue.IsEmpty )
                    {
                        // Peek the top item
                        if (state.RegisterWriteQueue.TryPeek(out var top))
                        {
                            // Decompose tuple
                            var (register, value) = top;

                            // Don't write the value through to the sensor IF it already has that value!
                            if (state.ConfigRegisterCache[register-FirstConfigRegister] != value) 
                            {
                                // Write out the desired value
                                state._client.WriteSingleRegister(state.Address, register, value);

                                // Update the cache, in case we report properties soon
                                state.ConfigRegisterCache[register - FirstConfigRegister] = value;

                                // If we have updated the address register, need to change our OWN
                                // view on where to look for this device.
                                // WARNING: The only way to make this change be remembered for next
                                // time is to change the initial state config.
                                if (register == AddressRegister)
                                    state.Address = value;
                            }

                            // Note that we don't DEQUEUE it until it's successfully been applied without error
                            state.RegisterWriteQueue.TryDequeue(out _);
                        }

                        // NOTE that we only do ONE change per run through telemetry
                    }
                    // Otherwise, get telemetry
                    else
                    {
                        try
                        {
                            state.InputRegisterCache = state._client.ReadHoldingRegisters<Int16>(state.Address,FirstDataRegister,AfterLastDataRegister-FirstDataRegister).ToArray();
                        }
                        catch
                        {
                            state.InputRegisterCache = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
    }

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
        // Take a copy in this thread of the input registers
        var inputs = InputRegisterCache;

        // If no readings yet, then we have no telemetry
        if (inputs is null)
            return null;

        // Save those as telemetry
        var reading = new Telemetry();
        reading.Temperature = (double)inputs[TemperatureRegister-FirstDataRegister] / 100.0;
        reading.Humidity = (double)inputs[HumidityRegister-FirstDataRegister] / 100.0;

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

            // NOTE: Changing address needs more conceptual think-through

            return Task.FromResult<object>(new());
        }

        throw new NotImplementedException($"Command {name} is not implemented on {dtmi}");
    }
 
    #endregion    
}