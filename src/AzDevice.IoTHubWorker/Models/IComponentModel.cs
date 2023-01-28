// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

namespace AzDevice.Models;

public interface IComponentModel
{
    string dtmi { get; }

    object SetProperty(string key, string jsonvalue);

    object GetProperties();

    object? GetTelemetry();

    Task<object> DoCommandAsync(string name, string jsonparams);

    void SetInitialState(IDictionary<string, string> values);
}
