// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

public interface IModbusClient
{
    Span<T> ReadHoldingRegisters<T>(int unitIdentifier, int startingAddress, int count) where T : unmanaged;
    void WriteSingleRegister(int unitIdentifier, int registerAddress, short value);

    void Connect();
}