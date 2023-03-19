// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

/// <summary>
/// Log events for Modbus interation
/// </summary>
public class ModbusLogEvents
{
    // 21. Modbus Create
    public const int ModbusCreateOK = 2100;
    public const int ModbusCreating = 2101;
    public const int ModbusCreateFailed = 2199;

    // 22. Modbus Read Holding
    public const int ModbusReadingHolding = 2201;
    
}