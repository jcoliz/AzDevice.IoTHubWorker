using AzDevice;
using AzDevice.Models;

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
