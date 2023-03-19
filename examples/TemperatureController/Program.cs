// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using AzDevice;
using AzDevice.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSystemd() 
    .ConfigureServices(services =>
    {
        services.AddHostedService<IoTHubWorker>();
        services.AddSingleton<IRootModel,ControllerModel>();
    })
    .ConfigureAppConfiguration(config =>
    {
        config.AddTomlFile("config.toml", optional: true, reloadOnChange: true);
    })
    .Build();

host.Run();
