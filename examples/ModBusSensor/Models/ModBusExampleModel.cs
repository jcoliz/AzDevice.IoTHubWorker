// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using FluentModbus;
using System.IO.Ports;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

public class ModBusExampleModel : IRootModel
{
    #region Properties

    public string? SerialNumber { get; private set; } = "Unassigned";

    public DateTimeOffset HeartbeatUTC => DateTimeOffset.UtcNow;

    public DateTimeOffset StartTimeUTC { get; } = DateTimeOffset.UtcNow;

    public string? SerialConnection { get; private set; }

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
    #endregion

    #region Commands
    private string[] GetSerialPortNames()
    {
        return SerialPort.GetPortNames();
    }

    #endregion

    #region Fields
    private ModbusRtuClient? _client = null;
    #endregion

    #region Log Identity
    /// <summary>
    /// How should this model appear in the logs?
    /// </summary>
    /// <returns>String to identify the current device</returns>
    public override string ToString()
    {
        return $"S/N:{SerialNumber ?? "null"} ver:{DeviceInformation.SoftwareVersion} sensor:{Sensor} uart:{SerialConnection ?? "null"}";
    }
    #endregion

    #region Constructor
    public ModBusExampleModel(ILogger<ModBusExampleModel> logger)
    {
        _logger = logger;
    }
    private readonly ILogger<ModBusExampleModel> _logger;
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
            "Info", 
            new DeviceInformationModel()
        },
        {
            "Sensor_1",
            new Xymd02Model()
        },
        {
            "Sensor_2",
            new SonbestSm7820Model()
        },
    };
    #endregion

    #region Internals
    private DeviceInformationModel DeviceInformation => (Components["Info"] as DeviceInformationModel)!;
    private Xymd02Model Sensor => (Components["Sensor_1"] as Xymd02Model)!;
    private SonbestSm7820Model Sensor2 => (Components["Sensor_2"] as SonbestSm7820Model)!;

    private void ConnectSerial()
    {
        try
        {
            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
                _client = null;
            }

            var config = SerialConnection!.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);

            _client = new ModbusRtuClient();

            // Default is 9600
            if (config.ContainsKey("baud"))
                _client.BaudRate = Convert.ToInt16(config["baud"]);

            // Default is Even
            if (config.ContainsKey("parity"))
                _client.Parity = Enum.Parse<Parity>(config["parity"]);

            // Default is One
            if (config.ContainsKey("stop"))
                _client.StopBits = Enum.Parse<StopBits>(config["stop"]);

            // Default is 1000 (milliseconds)
            if (config.ContainsKey("rto"))
                _client.ReadTimeout = Convert.ToInt16(config["rto"]);

            // Default is 1000 (milliseconds)
            if (config.ContainsKey("wto"))
                _client.WriteTimeout = Convert.ToInt16(config["wto"]);

            _client.Connect(config["port"],ModbusEndianness.BigEndian);

            Sensor.ModBusClient = _client;
            Sensor2.ModBusClient = _client;
        }
        catch (Exception ex)
        {
            var available = string.Join(',',GetSerialPortNames());
            if (string.IsNullOrEmpty(available))
                available = "NONE";
            var appex = new ApplicationException($"Unable to open {SerialConnection}. Available ports: {available}");
            throw new AggregateException(new [] { appex, ex });
        }
    }
    #endregion

    #region IComponentModel

    /// <summary>
    /// Identifier for this model
    /// </summary>
    [JsonIgnore]
    public string dtmi => "dtmi:azdevice:modbusexample;1";

    /// <summary>
    /// Get an object containing all current telemetry
    /// </summary>
    /// <returns>All telemetry we wish to send at this time, or null for don't send any</returns>
    object? IComponentModel.GetTelemetry()
    {
        // No telemetry on root component
        return null;
    }

    /// <summary>
    /// Set a particular property to the given value
    /// </summary>
    /// <param name="key">Which property</param>
    /// <param name="jsonvalue">Value to set (will be deserialized from JSON)</param>
    /// <returns>The unserialized new value of the property</returns>
    object IComponentModel.SetProperty(string key, string jsonvalue)
    {
        if (key == "TelemetryPeriod")
            return TelemetryPeriod = JsonSerializer.Deserialize<string>(jsonvalue)!;

        if (key == "SerialConnection")
        {
            var result = SerialConnection = JsonSerializer.Deserialize<string>(jsonvalue)!;
            ConnectSerial();
        }

        throw new NotImplementedException($"Property {key} is not implemented on {dtmi}");

    }

    /// <summary>
    /// Get an object containing all properties known to this model
    /// </summary>
    /// <returns>All known properties, and their current state</returns>
    object IComponentModel.GetProperties()
    {
        return this as ModBusExampleModel;
    }

    /// <summary>
    /// Set the application intitial state from the supplied configuration values
    /// </summary>
    /// <param name="values">Dictionary of all known configuration values which could apply to this component</param>
    void IComponentModel.SetInitialState(IDictionary<string, string> values)
    {
        if (values.ContainsKey("Version"))
            DeviceInformation.SoftwareVersion = values["Version"];

        if (values.ContainsKey("SerialNumber"))
            SerialNumber = values["SerialNumber"];

        if (values.ContainsKey("TelemetryPeriod"))
            TelemetryPeriod = values["TelemetryPeriod"];

        if (values.ContainsKey("SerialConnection"))
        {
            var value = values["SerialConnection"];
            if (value.Contains("port="))
            {
                SerialConnection = values["SerialConnection"];
                ConnectSerial();
            }
            else
            {
                var available = string.Join(',',GetSerialPortNames());
                if (string.IsNullOrEmpty(available))
                    available = "NONE";
                _logger.LogWarning(1071,"Uart Initial State: `{uart}` is invalid. Available ports: {ports}. Set desired SerialConnection property with correct configuration.",value,available);
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
        if (name == "GetSerialPortNames")
        {
            return Task.FromResult<object>(GetSerialPortNames());
        }
        throw new NotImplementedException($"Command {name} is not implemented on {dtmi}");
    }

    #endregion    
}