// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

namespace AzDevice.Models;

/// <summary>
/// Implements the root-level "default" model in an IoT Plug-and-Play system
/// </summary>
public interface IRootModel: IComponentModel
{
    /// <summary>
    /// How often to send telemetry, or zero to avoid sending any telemetry right now
    /// </summary>
    /// <remarks>
    /// Not required by IoT Plug-and-play. Still I this is a good convention which
    /// every solution should control
    /// </remarks>
    public TimeSpan TelemetryPeriod { get; }

    /// <summary>
    /// Optionally, the components which are contained within this one
    /// </summary>
    /// <remarks>
    /// Note that only the root model can have subcomponents
    /// </remarks>
    IDictionary<string,IComponentModel> Components { get; }
}
