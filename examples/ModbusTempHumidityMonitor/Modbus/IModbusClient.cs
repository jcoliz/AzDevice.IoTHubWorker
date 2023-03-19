// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

/// <summary>
/// Focused subset of modbus client used in this application
/// </summary>
public interface IModbusClient
{
    Span<T> ReadInputRegisters<T>(int unitIdentifier, int startingAddress, int count) where T : unmanaged;
    Span<T> ReadHoldingRegisters<T>(int unitIdentifier, int startingAddress, int count) where T : unmanaged;
    void WriteSingleRegister(int unitIdentifier, int registerAddress, short value);
    void Connect();
    bool IsConnected { get; }
}