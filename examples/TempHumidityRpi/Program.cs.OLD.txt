using System;
using System.Device.I2c;
using Iot.Device.Shtc3;

Console.WriteLine("TempHumidityRpi");

I2cConnectionSettings settings = new I2cConnectionSettings(1, Shtc3.DefaultI2cAddress);
I2cDevice device = I2cDevice.Create(settings);

using (Shtc3 sensor = new Shtc3(device))
{
    Console.WriteLine($"Sensor Id: {sensor.Id}");

    while (true)
    {
        try
        {
            if (sensor.TryGetTemperatureAndHumidity(out var temperature, out var relativeHumidity))
            {
                Console.WriteLine($"Temperature: {temperature.DegreesCelsius:0.#}\u00B0C");
                Console.WriteLine($"Relative humidity: {relativeHumidity.Percent:0.#}%");
                Console.WriteLine();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Failed. Will retry. ex:{ex.Message}");
        }

        Thread.Sleep(1000);
    }
}