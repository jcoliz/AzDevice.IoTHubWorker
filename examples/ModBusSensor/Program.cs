// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using AzDevice;
using AzDevice.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSystemd() 
    .ConfigureAppConfiguration(config =>
    {
        config.AddTomlFile("config.toml", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context,services) =>
    {
        services.AddHostedService<IoTHubWorker>();
        services.AddSingleton<IRootModel,ModBusExampleModel>();
        services.AddSingleton<IModbusClient, ModbusClient>();
        services.Configure<ModbusClientOptions>(
            context.Configuration.GetSection(ModbusClientOptions.Section)
        );
    })
    .Build();

host.Run();
