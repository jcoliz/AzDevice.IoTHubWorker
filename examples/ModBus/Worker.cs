using System.IO.Ports;
using FluentModbus;

namespace ModBus;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ModbusRtuClient _client;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;

        string[] ports = SerialPort.GetPortNames();
        var portsall = string.Join(',', ports);
        _logger.LogInformation("Available ports: {ports}", portsall);

        _client = new ModbusRtuClient()
        {
            BaudRate = 9600,
            Parity = Parity.None,
            StopBits = StopBits.One
        };

        _client.Connect("COM3",ModbusEndianness.BigEndian);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var inputs = _client.ReadInputRegisters<Int16>(1,1,2).ToArray();

            _logger.LogInformation("Temp: {temp}, Humidity: {rh}", inputs[0], inputs[1]);

            var holdings = _client.ReadHoldingRegisters<Int16>(1,0x101,4).ToArray();

            _logger.LogInformation("Address: {addr}, Baud: {baud}, Temp Cor {tc}, Humidity Cor {hc}", holdings[0], holdings[1], holdings[2], holdings[3]);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
