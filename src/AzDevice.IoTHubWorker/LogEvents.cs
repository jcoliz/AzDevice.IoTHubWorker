// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

namespace AzDevice;

public class LogEvents
{
    // Conventions:
    // xx__: Which major section of the application is it in
    // __9_: Critical failures. Unrecoverable errors which will cause application to exit
    // __8_: Errors
    // __7_: Warnings
    // __00: OK. Major section complete
    // __[1-6][1-9]: Generally should be debug-level messages. Put the big stuff in the OK log

    // 1. Execute
    public const int ExecuteStartOK     = 100;
    public const int ExecuteDeviceInfo  = 101;
    public const int ExecuteDeviceModel = 102;
    public const int ExecuteFinished    = 110;
    public const int ExecuteFailed      = 190;

    // 2. Config
    public const int ConfigLoaded       = 201;
    public const int ConfigOK           = 200;
    public const int ConfigError        = 280;

    // 3. Provision
    public const int ProvisionConfig    = 301;
    public const int ProvisionInit      = 302;
    public const int ProvisionStatus    = 303;
    public const int ProvisionOK        = 300;
    public const int ProvisionError     = 380;
    public const int ProvisionFailed    = 390;

    // 4. Connect
    public const int ConnectAuth        = 401;
    public const int ConnectOK          = 400;
    public const int ConnectError       = 480;

    // 5. Telemetry
    public const int TelemetryOK        = 500;
    public const int TelemetrySentOne   = 501;
    public const int TelemetrySentRoot  = 502;
    public const int TelemetryNotSent   = 570;

    // 10. Commands
    public const int CommandOK         = 1100;
    public const int CommandReceived   = 1101;
    public const int CommandSingleFailure = 1181;
    public const int CommandMultipleFailure = 1182;
 
    // 20. Properties
    public const int PropertyOK         = 2000;
    public const int PropertyRequest = 2001;
    public const int PropertyResponse = 2002;
    public const int PropertySendActuals = 2003;
    public const int PropertyUpdateFailure = 2081;
    public const int PropertySingleFailure = 2081;
    public const int PropertyMultipleFailure = 2082;
    public const int PropertyComponentOK = 2300;
}