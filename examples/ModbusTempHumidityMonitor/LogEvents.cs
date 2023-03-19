// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

/// <summary>
/// Application-specific log events
/// </summary>
public class LogEvents
{
    // 1x. Root Model
    // 2x. Modbus

    // 3x. Sensor generally

    public const int SensorReady = 3001;

    // 4x. SM7820
    // 40. SM7820 Background worker
    public const int BackgroundInputsError = 4086;
    public const int BackgroundCacheError = 4087;
    public const int BackgroundError = 4088;
}
