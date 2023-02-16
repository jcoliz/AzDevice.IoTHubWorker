// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using FluentModbus;
using Microsoft.Extensions.Options;
using System.IO.Ports;

namespace AzDevice;

/// <summary>
/// Modbus client wrapper for use in dependency injection
/// </summary>
public class ModbusClient : IModbusClient
{
    private readonly IOptions<ModbusClientOptions> _options;
    private readonly ILogger<ModbusClient> _logger;
    private readonly ModbusRtuClient _client;
    private bool connected = false;

    public ModbusClient(IOptions<ModbusClientOptions> options, ILogger<ModbusClient> logger)
    {
        _options = options;
        _logger = logger;
        _client = new ModbusRtuClient();
    }

    public void Connect()
    {
        if (connected)
            return;

        _logger.LogDebug(ModbusLogEvents.ModbusCreating, "Creating with options {options}", _options.Value);

        // Default is 9600
        if (_options.Value.BaudRate.HasValue)
            _client.BaudRate = _options.Value.BaudRate.Value;

        // Default is Even
        if (_options.Value.Parity is not null)
            _client.Parity = Enum.Parse<Parity>(_options.Value.Parity);

        // Default is One
        if (_options.Value.StopBits is not null)
            _client.StopBits = Enum.Parse<StopBits>(_options.Value.StopBits);

        // Default is 1000 (milliseconds)
        if (_options.Value.ReadTimeout.HasValue)
            _client.ReadTimeout = _options.Value.ReadTimeout.Value;

        // TODO: Allow config of write timeout

        _client.Connect(_options.Value.Port!,ModbusEndianness.BigEndian);
        connected = true;

        _logger.LogInformation(ModbusLogEvents.ModbusCreateOK, "Created OK");
    }

    public Span<T> ReadHoldingRegisters<T>(int unitIdentifier, int startingAddress, int count) where T : unmanaged
    {
        return _client.ReadHoldingRegisters<T>(unitIdentifier, startingAddress, count);
    }

    public void WriteSingleRegister(int unitIdentifier, int registerAddress, short value)
    {
        _client.WriteSingleRegister(unitIdentifier, registerAddress, value);
    }
}
