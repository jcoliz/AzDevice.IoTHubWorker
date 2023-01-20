using AzDevice;
using AzDevice.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSystemd() 
    .ConfigureServices(services =>
    {
        services.AddHostedService<IoTHubWorker>();
        services.AddSingleton<IRootModel>(new ControllerModel());
    })
    .ConfigureHostConfiguration(config =>
    {
        config.AddTomlFile("config.toml", optional: true, reloadOnChange: true);
        config.AddJsonFile("version.json", optional: true, reloadOnChange: true);
    })
    .Build();

host.Run();
