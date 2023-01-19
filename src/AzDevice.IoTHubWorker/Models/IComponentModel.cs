// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

namespace AzDevice.Models;

public interface IComponentModel
{
    string dtmi { get; }

    bool HasTelemetry { get; }

    object SetProperty(string key, object value);

    object GetProperties();

    IDictionary<string,object> GetTelemetry();

    Task<object> DoCommandAsync(string name, string jsonparams);

    void SetInitialState(IDictionary<string, string> values);
}
