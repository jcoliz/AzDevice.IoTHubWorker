// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AzDevice;

/// <summary>
/// Solution-independent background service for managing connection
/// to Azure IoT Hub from a device
/// </summary>
/// <remarks>
/// Implements IoT Plug and Play conventions.
/// Connects using DPS using the same config.toml as Azure IoT Edge
/// </remarks>

public sealed class IoTHubWorker : BackgroundService
{
#region Injected Fields

    private readonly IRootModel _model;
    private readonly ILogger<IoTHubWorker> _logger;
    private readonly IConfiguration _config;

    #endregion

    #region Fields
    private DeviceClient? iotClient;
    private SecurityProviderSymmetricKey? security;
    private DeviceRegistrationResult? result;
#endregion

#region Constructor
    public IoTHubWorker(ILogger<IoTHubWorker> logger, IRootModel model, IConfiguration config)
    {
        _logger = logger;
        _model = model;
        _config = config;
    }
#endregion

#region Execute
    /// <summary>
    /// Do the work of this worker
    /// </summary>
    /// <param name="stoppingToken">Cancellation to indicate it's time stop</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation(LogEvents.ExecuteStartOK,"Started OK");

            if (!string.IsNullOrEmpty(_model.dtmi))
                _logger.LogInformation(LogEvents.ExecuteDeviceModel,"Model: {model}", _model.dtmi);

            await LoadInitialState();
            var device = _model.DeviceInfo;
            _logger.LogInformation(LogEvents.ExecuteDeviceInfo,"Device: {mfr} {model} {version}", device.Manufacturer, device.DeviceModel, device.SoftwareVersion);

            await ProvisionDevice();
            await OpenConnection();
            while (!stoppingToken.IsCancellationRequested)
            {
                await SendTelemetry();
                await Task.Delay(_model.TelemetryPeriod, stoppingToken);
            }

            if (iotClient is not null)
                await iotClient.CloseAsync();
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation(LogEvents.ExecuteFinished,"IoTHub Device Worker: Stopped");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ExecuteFailed,"IoTHub Device Worker: Failed {type} {message}", ex.GetType().Name, ex.Message);
        }
    }
#endregion

#region Startup
    /// <summary>
    /// Loads initial state of components out of config "InitialState" section
    /// </summary>
    /// <returns></returns>
    private Task LoadInitialState()
    {
        try
        {
            var initialstate = _config.GetSection("InitialState");
            if (initialstate.Exists())
            {
                int numkeys = 0;
                var root = initialstate.GetSection("Root");
                if (root.Exists())
                {
                    var dictionary = root.GetChildren().ToDictionary(x => x.Key, x => x.Value);
                    _model.SetInitialState(dictionary);
                    numkeys += dictionary.Keys.Count;
                }

                foreach(var component in _model.Components)
                {
                    var section = initialstate.GetSection(component.Key);
                    if (section.Exists())
                    {
                        var dictionary = section.GetChildren().ToDictionary(x => x.Key, x => x.Value);
                        component.Value.SetInitialState(dictionary);
                        numkeys += dictionary.Keys.Count;
                    }
                }

                _logger.LogInformation(LogEvents.ConfigOK,"Initial State: OK Applied {numkeys} keys",numkeys);
            }
            else
            {
                _logger.LogWarning(LogEvents.ConfigNoExists,"Initial State: Not specified");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ConfigError,"Initial State: Error {message}", ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Provision this device according to the config supplied in "Provisioning" section
    /// </summary>
    /// <remarks>
    /// Note that the Provisioning section is designed to follow the format of the config.toml
    /// used by Azure IoT Edge. So if you generate a config.toml for an edge device, you can
    /// use it here. Just be sure to add the config.toml to the HostConfiguration during setup.
    /// (See examples for how this is done.)
    /// </remarks>
    /// <exception cref="ApplicationException">Thrown if provisioning fails (critical error)</exception>
    private async Task ProvisionDevice()
    {
        string GetConfig(string key)
        {
            return _config[key] ?? throw new ApplicationException($"Failed. Please supply {key} in configuration");
        }

        try
        {
            var source = GetConfig("Provisioning:source");
            var global_endpoint = GetConfig("Provisioning:global_endpoint");
            var id_scope = GetConfig("Provisioning:id_scope"); 
            var method = GetConfig("Provisioning:attestation:method");
            var registration_id = GetConfig("Provisioning:attestation:registration_id");
            var symmetric_key_value = GetConfig("Provisioning:attestation:symmetric_key:value");

            if (source != "dps")
                throw new ApplicationException($"Failed: source {source} not supported");
            if (method != "symmetric_key")
                throw new ApplicationException($"Failed: method {method} not supported");

            _logger.LogDebug(LogEvents.ProvisionConfig,"Provisioning: Found config for source:{source} method:{method}", source, method);

            security = new SecurityProviderSymmetricKey(
                registration_id,
                symmetric_key_value,
                null);

            using ProvisioningTransportHandler transportHandler = new ProvisioningTransportHandlerHttp();

            var endpoint = new Uri(global_endpoint);
            var provClient = ProvisioningDeviceClient.Create(
                endpoint.Host,
                id_scope,
                security,
                transportHandler);

            _logger.LogDebug(LogEvents.ProvisionInit,"Provisioning: Initialized {id}", security.GetRegistrationID());

            result = await provClient.RegisterAsync();

            _logger.LogDebug(LogEvents.ProvisionStatus,"Provisioning: Status {status}", result.Status);
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                throw new ApplicationException($"Failed. Status: {result.Status} {result.Substatus}");
            }

            _logger.LogInformation(LogEvents.ProvisionOK,"Provisioning: OK. Device {id} on Hub {hub}", result.DeviceId, result.AssignedHub);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ProvisionError,"Provisioning: Error {message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Open a connection to IoT Hub
    /// </summary>
    /// <exception cref="ApplicationException">Thrown if connection fails (critical error)</exception>
    private async Task OpenConnection()
    {
        try
        {
            // NOTE we can now store the device registration result to storage, and use it next time
            // to not have to run the above registration flow again

            IAuthenticationMethod auth = new DeviceAuthenticationWithRegistrySymmetricKey(
                result!.DeviceId,
                security!.GetPrimaryKey());
            _logger.LogDebug(LogEvents.ConnectAuth,"Connection: Created SK Auth");

            var options = new ClientOptions
            {
                ModelId = _model.dtmi
            };

            iotClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Mqtt, options);
            _logger.LogInformation(LogEvents.ConnectOK,"Connection: OK. {info}", iotClient.ProductInfo);

            // Update the current state of actual properties
            await UpdateReportedProperties();

            // Read the current state of desired properties and set the local values as desired
            var twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
            await OnDesiredPropertiesUpdate(twin.Properties.Desired, this);

            // Attach a callback for updates to the module twin's desired properties.
            await iotClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Register callback for health check command
            await iotClient.SetMethodDefaultHandlerAsync(OnCommandReceived, null);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ConnectError,"Connection: Error {message}", ex.Message);
            throw new ApplicationException("Connection to IoT Hub failed", ex);
        }
    }
    #endregion

    #region Commands
    /// <summary>
    /// Receive a command from IoT Hub
    /// </summary>
    /// <param name="methodRequest">Details of the command (method) request</param>
    /// <param name="userContext">Additional context (unused)</param>
    /// <returns>Command-specific result payload</returns>
    private async Task<MethodResponse> OnCommandReceived(MethodRequest methodRequest, object userContext)
    {
        var command = methodRequest.Name;
        try
        {
            _logger.LogDebug(LogEvents.CommandReceived,"Command: Received {command}", methodRequest.Name);

            // By default, this is a command for the root
            IComponentModel component = _model;
            object? result = null;

            // Unless the command has multiple tokens
            var split = methodRequest.Name.Split('*');
            if (split.Skip(1).Any())
            {
                // Find the named component
                var componentname = split[0];
                var components = _model.Components.Where(x=>x.Key == componentname);
                if (!components.Any())
                    throw new ApplicationException($"Unknown component: {componentname}");
                else if (components.Skip(1).Any())
                    throw new ApplicationException($"Ambiguous component: {componentname}");
                component = components.Single().Value;
                command = split[1];
            }

            var datajson = Encoding.UTF8.GetString(methodRequest.Data);
            result = await component.DoCommandAsync(command,datajson);
            var resultjson = JsonSerializer.Serialize(result);
            var response = Encoding.UTF8.GetBytes(resultjson);

            _logger.LogInformation(LogEvents.CommandOK,"Command: OK {command} Response: {response}", methodRequest.Name,resultjson);

            return new MethodResponse(response, (int)HttpStatusCode.OK);
        }
        catch (AggregateException ex)
        {
            foreach (Exception exception in ex.InnerExceptions)
            {
                _logger.LogError(LogEvents.CommandMultipleFailure, exception, "Command: {command} Multiple failures", command);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.CommandSingleFailure,ex,"Command: {command} Failed", command);
        }
        return new MethodResponse(Encoding.UTF8.GetBytes("{}"), (int)HttpStatusCode.InternalServerError);
    }
    #endregion

    #region Telemetry
    /// <summary>
    /// Send latest telemetry from root and all components
    /// </summary>
    private async Task SendTelemetry()
    {
        int numsent = 0;

        // Send telementry from root

        if (_model.HasTelemetry)
        {
            // Obtain readings from this component
            var readings = _model.GetTelemetry();

            // Send them
            await SendTelemetryMessageAsync(readings, null);
            ++numsent;
        }

        // Send telemetry from components

        foreach(var kvp in _model.Components)
        {
            if (kvp.Value.HasTelemetry)
            {
                // Note that official PnP messages can only come from a single component at a time.
                // This is a weakness that drives up the message count. So, will have to decide later
                // if it's worth keeping this, or scrapping PnP compatibility and collecting them all
                // into a single message.

                // Obtain readings from this component
                var readings = kvp.Value.GetTelemetry();

                // Send them
                await SendTelemetryMessageAsync(readings, kvp);
                ++numsent;
            }
        }

        if (numsent > 0)
            _logger.LogInformation(LogEvents.TelemetryOK,"Telemetry: OK {count} messages",numsent);            
        else
            _logger.LogWarning(LogEvents.TelemetryNotSent,"Telemetry: No components had available readings. Nothing sent");
    }

    private async Task SendTelemetryMessageAsync(IDictionary<string, object> telemetryPairs, KeyValuePair<string, IComponentModel>? component = default)
    {
        // Make a message out of it
        using var message = CreateTelemetryMessage(telemetryPairs,component?.Key);

        // Send the message
        await iotClient!.SendEventAsync(message);
        var detailslist = telemetryPairs.Select(x=>$"{x.Key}={x.Value:F1}");
        var details = string.Join(' ',detailslist);
        if (component is null)
            _logger.LogDebug(LogEvents.TelemetrySentRoot,"Telemetry: Root {details}", details);
        else
            _logger.LogDebug(LogEvents.TelemetrySentOne,"Telemetry: {component} {id} {details}", component?.Key, component?.Value, details);
    }

    // Below is from https://github.com/Azure/azure-iot-sdk-csharp/blob/1e97d800061aca1ab812ea32d47bac2442c1ed26/iothub/device/samples/solutions/PnpDeviceSamples/PnpConvention/PnpConvention.cs#L40

    /// <summary>
    /// Create a plug and play compatible telemetry message.
    /// </summary>
    /// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
    /// <param name="telemetryPairs">The unserialized name and value telemetry pairs, as defined in the DTDL interface. Names must be 64 characters or less. For more details see
    /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
    /// <param name="encoding">The character encoding to be used when encoding the message body to bytes. This defaults to utf-8.</param>
    /// <returns>A plug and play compatible telemetry message, which can be sent to IoT Hub. The caller must dispose this object when finished.</returns>
    public static Message CreateTelemetryMessage(IDictionary<string, object> telemetryPairs, string? componentName = default, Encoding? encoding = default)
    {
        if (telemetryPairs == null)
        {
            throw new ArgumentNullException(nameof(telemetryPairs));
        }

        Encoding messageEncoding = encoding ?? Encoding.UTF8;
        string payload = JsonSerializer.Serialize(telemetryPairs);
        var message = new Message(messageEncoding.GetBytes(payload))
        {
            ContentEncoding = messageEncoding.WebName,
            ContentType = ContentApplicationJson,
        };

        if (!string.IsNullOrWhiteSpace(componentName))
        {
            message.ComponentName = componentName;
        }

        return message;
    }
    private const string ContentApplicationJson = "application/json";
#endregion

#region Properties
    /// <summary>
    /// Receive an update from IoT Hub that some property/ies have changed.
    /// </summary>
    /// <remarks>
    /// Will send an update(s) to acknowledge each change.
    /// </remarks>
    /// <param name="desiredProperties">Details of the property change</param>
    /// <param name="userContext">Additional context (unused)</param>
    private async Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
    {
        try
        {
            _logger.LogDebug(LogEvents.PropertyRequest, "Property: Desired {request}",desiredProperties.ToJson());

            // Consider each kvp in the request
            foreach(KeyValuePair<string, object> prop in desiredProperties)
            {
                var fullpropname = "(unknown)";
                try
                {
                    fullpropname = prop.Key;

                    // Is this 'property' actually one of our components?
                    if (_model.Components.ContainsKey(prop.Key))
                    {
                        // In which case, we need to iterate again over the desired property's children
                        var component = _model.Components[prop.Key];
                        var jo = prop.Value as JObject;
                        foreach(JProperty child in jo!.Children())
                        {
                            if (child.Name != "__t")
                            {
                                fullpropname = $"{prop.Key}.{child.Name}";

                                // Update the property
                                var updated = component.SetProperty(child.Name,child.Value.ToString());
                                _logger.LogInformation(LogEvents.PropertyComponentOK,"Property: Component OK. Updated {property} to {updated}",fullpropname,updated);

                                // Acknowledge the request back to hub
                                await RespondPropertyUpdate(fullpropname,updated,desiredProperties.Version);
                            }
                        }
                    }
                    // Otherwise, treat it as a property of the rool model
                    else
                    {
                        // Update the property
                        var updated = _model.SetProperty(prop.Key,prop.Value.ToString()!);
                        _logger.LogInformation(LogEvents.PropertyOK,"Property: OK. Updated {property} to {updated}",fullpropname,updated);

                        // Acknowledge the request back to hub
                        await RespondPropertyUpdate(fullpropname,updated,desiredProperties.Version);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(LogEvents.PropertyUpdateFailure,ex,"Property: Update failed for {property}",fullpropname);
                }
            }
        }
        catch (AggregateException ex)
        {
            foreach (Exception exception in ex.InnerExceptions)
            {
                _logger.LogError(LogEvents.PropertyMultipleFailure, exception, "Property: Multiple update failures");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.PropertySingleFailure,ex,"Property: Update failed");
        }
    }

    private async Task RespondPropertyUpdate(string key, object value, long version)
    {
        // Key is either 'Property' or 'Component.Property'
        var split = key.Split('.');
        string property = split.Last();
        string? component = split.SkipLast(1).FirstOrDefault();

        var patch = new Dictionary<string,object>();
        var ack = new PropertyChangeAck() 
        {
            PropertyValue = value,
            AckCode = HttpStatusCode.OK,
            AckVersion = version,
            AckDescription = "OK"
        };
        patch.Add(property,ack);
        var response = patch;
        
        if (component is not null)
        {
            patch.Add("__t","c");
            
            response = new Dictionary<string,object>()
            {
                { component, patch }
            };
        }

        var json = JsonSerializer.Serialize(response);
        var resulttc = new TwinCollection(json);
        await iotClient!.UpdateReportedPropertiesAsync(resulttc);

        _logger.LogDebug(LogEvents.PropertyResponse, "Property: Responded to server with {response}",json);
    }

    /// <summary>
    ///  Single update of all reported properties at once
    /// </summary>
    private async Task UpdateReportedProperties()
    {
        // Create dictionary of root properties
        var root = _model.GetProperties();
        var j1 = JsonSerializer.Serialize(root);
        var d1 = JsonSerializer.Deserialize<Dictionary<string, object>>(j1);

        // Create dictionary of components with their properties
        var d2 = _model.Components.ToDictionary(x => x.Key, x => x.Value.GetProperties());

        // Merge them
        var d3 = new[] { d1, d2 };
        var update = d3.SelectMany(x => x!).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Convert to json and send them
        var json = JsonSerializer.Serialize(update);
        var resulttc = new TwinCollection(json);
        await iotClient!.UpdateReportedPropertiesAsync(resulttc);

        _logger.LogDebug(LogEvents.PropertySendActuals,"Property: Updated reported properties as {update}",json);
    }
    #endregion
}
