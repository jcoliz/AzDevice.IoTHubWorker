// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

namespace AzDevice.Models;

public interface IRootModel: IComponentModel
{
    public TimeSpan TelemetryPeriod { get; }

    IDictionary<string,IComponentModel> Components { get; }
}
