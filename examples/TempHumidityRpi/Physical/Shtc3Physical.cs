using System.Device.I2c;
using System.Text.Json.Serialization;
using Iot.Device.Shtc3;

/// <summary>
/// SHTC3 - Temperature & Humidity Sensor
/// </summary>
public class Shtc3Physical: IDisposable
{
    private readonly I2cConnectionSettings _settings;
    private readonly I2cDevice _device;
    private readonly Shtc3 _sensor;
    private bool disposedValue;

    [JsonPropertyName("Temperature")]
    public double Temperature { get; private set; }

    [JsonPropertyName("Humidity")]
    public double Humidity { get; private set; }

    [JsonIgnore]
    public int Id { get; private set; }

    public Shtc3Physical()
    {
        _settings = new I2cConnectionSettings(1, Shtc3.DefaultI2cAddress);
        _device = I2cDevice.Create(_settings);
        _sensor = new Shtc3(_device);
        Id = _sensor.Id ?? int.MinValue;
    }

    public bool TryUpdate()
    {
        var result = false;
        try
        {
            if (_sensor.TryGetTemperatureAndHumidity(out var temperature, out var relativeHumidity))
            {
                Temperature = temperature.DegreesCelsius;
                Humidity = relativeHumidity.Percent;
                result = true;
            }
        }
        catch
        {
        }

        return result;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                _sensor.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Shtc3Physical()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}