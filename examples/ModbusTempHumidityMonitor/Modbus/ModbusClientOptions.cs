// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

/// <summary>
/// Options for configuring the modbus client
/// </summary>
public class ModbusClientOptions
{
    public const string Section = "ModbusClient";

    public string? Port { get; set; } = null;
    public int? BaudRate { get; set; } = null;
    public string? Parity { get; set; } = null;
    public string? StopBits { get; set; } = null;
    public int? ReadTimeout { get; set; } = null;

    public override string ToString()
    {
        return $"Port={Port ?? "null"};BaudRate={BaudRate?.ToString() ?? "null"};Parity={Parity ?? "null"};StopBits={StopBits ?? "null"};ReadTimeout={ReadTimeout?.ToString() ?? "null"}";
    }
}
