// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

namespace AzDevice.Models;

public interface IRootModel: IComponentModel
{
    public TimeSpan TelemetryPeriod { get; }

    public DeviceInformationModel DeviceInfo { get; }

    IDictionary<string,IComponentModel> Components { get; }

    Task<string> LoadConfigAsync();
}
