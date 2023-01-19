using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace AzDevice.Models;

public class DeviceInformationModel
{
    // Manufacturer of the device THIS CODE is running on

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    [JsonPropertyName("manufacturer")]
    public string Manufacturer => "BrewHub";

    [JsonPropertyName("model")]
    public string? DeviceModel => "Digital Distillery Controller DC-01";

    [JsonPropertyName("swVersion")]
    public string? SoftwareVersion { get; set; } = "0.0.1";

    [JsonPropertyName("osName")]
    public string OperatingSystemName => RuntimeInformation.OSDescription;

    [JsonPropertyName("processorArchitecture")]
    public string ProcessorArchitecture => RuntimeInformation.OSArchitecture.ToString();

    [JsonPropertyName("totalStorage")]
    public double AvailableStorageKB 
    {
        get
        {
            double result = 0;
            var drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady)
                {
                    result += (double)drive.AvailableFreeSpace / 1024.0;
                }
            }
            return result;
        }
    }

    [JsonPropertyName("totalMemory")]
    public double AvailableMemoryKB
    {
        get
        {
            var tm = GC.GetGCMemoryInfo();
            var mem = tm.TotalAvailableMemoryBytes;

            return (double)mem / 1024.0;
        }
    }
}