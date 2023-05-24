// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using AzDevice.Models;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AzDevice;

public class InfluxDbWorker : BackgroundService
{
#region Injected Fields

    private readonly IRootModel _model;
    private readonly ILogger<IoTHubWorker> _logger;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _hostenv;
    private readonly IHostApplicationLifetime _lifetime;

    #endregion

    #region Fields
    private InfluxDBClient? client;
    private string? org;
    private string? bucket;
    private string? deviceid;
    private DateTimeOffset NextPropertyUpdateTime = DateTimeOffset.MinValue;
    private TimeSpan PropertyUpdatePeriod = TimeSpan.FromMinutes(1);
    private readonly TimeSpan TelemetryRetryPeriod = TimeSpan.FromMinutes(1);
    #endregion

    #region Constructor
    public InfluxDbWorker(ILogger<IoTHubWorker> logger, IRootModel model, IConfiguration config, IHostEnvironment hostenv, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _model = model;
        _config = config;
        _hostenv = hostenv;
        _lifetime = lifetime;
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
            var done = _config["run"] == "once";

            _logger.LogInformation(LogEvents.ExecuteStartOK,"Started OK");

            await LoadInitialState();

            _logger.LogInformation(LogEvents.ExecuteDeviceInfo,"Device: {device}", _model);
            _logger.LogInformation(LogEvents.ExecuteDeviceModel,"Model: {dtmi}", _model.dtmi);

            await ProvisionDevice();
            await OpenConnection();
            while (!stoppingToken.IsCancellationRequested)
            {
                await SendTelemetry();
                await UpdateReportedProperties();

                if (done)
                    break;

                await Task.Delay(_model.TelemetryPeriod > TimeSpan.Zero ? _model.TelemetryPeriod : TelemetryRetryPeriod, stoppingToken);
            }

            if (client is not null)
            {
                client.Dispose();
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation(LogEvents.ExecuteFinished,"IoTHub Device Worker: Stopped");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ExecuteFailed,"IoTHub Device Worker: Failed {type} {message}", ex.GetType().Name, ex.Message);
        }

        await Task.Delay(500);
        _lifetime.StopApplication();
    }
#endregion

#region Startup
    /// <summary>
    /// Loads initial state of components out of config "InitialState" section
    /// </summary>
    /// <returns></returns>
    protected Task LoadInitialState()
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

            // We allow for the build system to inject a resource named "version.txt"
            // which contains the software build version. If it's not here, we'll just
            // continue with no version information.
            var assembly = Assembly.GetAssembly(_model.GetType());
            var resource = assembly!.GetManifestResourceNames().Where(x => x.EndsWith(".version.txt")).SingleOrDefault();
            if (resource is not null)
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                using var streamreader = new StreamReader(stream!);
                var version = streamreader.ReadLine();

                // Where to store the software build version is solution-dependent.
                // Thus, we will pass it in as a root-level "Version" initial state, and let the
                // solution decide what to do with it.
                _model.SetInitialState(new Dictionary<string,string>() {{ "Version", version! }});
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ConfigFailed,"Initial State: Failed {message}", ex.Message);
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
    protected Task ProvisionDevice()
    {
        string GetConfig(string key)
        {
            return _config[key] ?? throw new ApplicationException($"Failed. Please supply {key} in configuration");
        }

        try
        {
            if (!_config.GetSection("Provisioning").Exists())
                throw new ApplicationException($"Unable to find provisioning details in app configuration. Create a config.toml with provisioning details in the content root ({_hostenv.ContentRootPath}).");

            deviceid = GetConfig("Provisioning:deviceid");

            _logger.LogInformation(LogEvents.ProvisionOK,"Provisioning: OK. Device {id}", deviceid);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ProvisionError,"Provisioning: Error {message}", ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Open a connection to InfluxDB
    /// </summary>
    /// <exception cref="ApplicationException">Thrown if connection fails (critical error)</exception>
    protected async Task OpenConnection()
    {
        string GetConfig(string key)
        {
            return _config[key] ?? throw new ApplicationException($"Failed. Please supply {key} in configuration");
        }

        try
        {
            if (!_config.GetSection("InfluxDB").Exists())
                throw new ApplicationException($"Unable to find InfluxDB details in app configuration. Create a config.toml with provisioning details in the content root ({_hostenv.ContentRootPath}).");

            var url = GetConfig("InfluxDB:url");
            var token = GetConfig("InfluxDB:token");
            org = GetConfig("InfluxDB:org");
            bucket = GetConfig("InfluxDB:bucket");

            client = new InfluxDBClient(url, token);
            if (! await client.PingAsync())
                throw new ApplicationException($"Unable to ping InfluxDB on {url}");

            var version = client.VersionAsync();
            _logger.LogInformation(LogEvents.ConnectOK,"Connection: OK. InfluxDB ver.{ver} on {url}",version,url);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.ConnectFailed,"Connection: Failed {message}", ex.Message);
            throw new ApplicationException("Connection to IoT Hub failed", ex);
        }
    }
    #endregion

    #region Commands
    #endregion

    #region Telemetry
    /// <summary>
    /// Send latest telemetry from root and all components
    /// </summary>
    protected async Task SendTelemetry()
    {
        try
        {
            int numsent = 0;

            // Send telementry from root

            if (_model.TelemetryPeriod > TimeSpan.Zero)
            {
                // Obtain readings from the root
                var readings = _model.GetTelemetry();

                // If telemetry exists
                if (readings is not null)
                {
                    // Send them
                    await SendTelemetryMessageAsync(readings, new(string.Empty, _model));
                    ++numsent;
                }

                // Send telemetry from components

                foreach(var kvp in _model.Components)
                {
                    // Obtain readings from this component
                    readings = kvp.Value.GetTelemetry();
                    if (readings is not null)
                    {
                        // Note that official PnP messages can only come from a single component at a time.
                        // This is a weakness that drives up the message count. So, will have to decide later
                        // if it's worth keeping this, or scrapping PnP compatibility and collecting them all
                        // into a single message.

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
            else
                _logger.LogWarning(LogEvents.TelemetryNoPeriod,"Telemetry: Telemetry period not configured. Nothing sent. Will try again in {period}",TelemetryRetryPeriod);
        }
        catch (AggregateException ex)
        {
            foreach (Exception exception in ex.InnerExceptions)
            {
                _logger.LogError(LogEvents.TelemetryMultipleError, exception, "Telemetry: Multiple Errors");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.TelemetrySingleError,ex,"Telemetry: Error");
        }
    }

    private Task SendTelemetryMessageAsync(object telemetry, KeyValuePair<string, IComponentModel> component = default)
    {
        using (var writeApi = client!.GetWriteApi())
        {
            // Make a message out of it
            var point = PointData.Measurement($"tel-{component.Value.dtmi}")
                .Tag("device", deviceid)
                .Tag("component", component.Key)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ms);
            
            var j1 = JsonSerializer.Serialize(telemetry);
            var d1 = JsonSerializer.Deserialize<Dictionary<string, object>>(j1);
            foreach(var kvp in d1!)
            {
                var el = (JsonElement)kvp.Value;
                point = point.Field(kvp.Key,el.GetDouble());
            }

            // Send the message
            writeApi.WritePoint(point, bucket, org);                
        }

        return Task.CompletedTask;
    }
#endregion

#region Properties
    /// <summary>
    ///  Single update of all reported properties at once
    /// </summary>
    private Task UpdateReportedProperties()
    {
        try
        {
            if (DateTimeOffset.Now < NextPropertyUpdateTime)
                return Task.CompletedTask;

            // Create dictionary of root properties
            var root = _model.GetProperties();
            var j1 = JsonSerializer.Serialize(root);
            var d1 = JsonSerializer.Deserialize<Dictionary<string, object>>(j1);

            // Create dictionary of components with their properties
            var d2 = _model.Components.ToDictionary(x => x.Key, x => x.Value.GetProperties());

            // Merge them
            var d3 = new[] { d1, d2 }; 
            var update = d3.SelectMany(x => x!).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

#if false
            // Convert to json and send them
            var json = JsonSerializer.Serialize(update);
            var resulttc = new TwinCollection(json);
            await iotClient!.UpdateReportedPropertiesAsync(resulttc);
            _logger.LogDebug(LogEvents.PropertyReportedDetail,"Property: Reported details {detail}. Next update after {delay}",json,PropertyUpdatePeriod);
#endif

            _logger.LogInformation(LogEvents.PropertyReportedOK,"Property: OK Reported {count} properties",update.Count);

            // Manage back-off of property updates
            NextPropertyUpdateTime = DateTimeOffset.Now + PropertyUpdatePeriod;
            PropertyUpdatePeriod += PropertyUpdatePeriod;
            TimeSpan oneday = TimeSpan.FromDays(1);
            if (PropertyUpdatePeriod > oneday)
                PropertyUpdatePeriod = oneday;
        }
        catch (ApplicationException ex)
        {
            // An application exception is a soft error. Don't need to log the whole exception,
            // just give the message and move on
            _logger.LogError(LogEvents.PropertyReportApplicationError,"Property: Application Error. {message}", ex.Message);
        }
        catch (AggregateException ex)
        {
            foreach (Exception exception in ex.InnerExceptions)
            {
                _logger.LogError(LogEvents.PropertyReportMultipleErrors, exception, "Property: Multiple reporting errors");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.PropertyReportSingleError,ex,"Property: Reporting error");
        }

        return Task.CompletedTask;
    }
    #endregion
}
