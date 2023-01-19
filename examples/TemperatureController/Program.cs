using AzDevice;
using AzDevice.Models;
using Alexinea.Extensions.Configuration.Toml;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<IoTHubWorker>();
        services.AddSingleton<IRootModel>(new ControllerModel());
    })
    .ConfigureHostConfiguration(config =>
    {
        config.AddTomlFile("config.toml");
    })
    .Build();

host.Run();
