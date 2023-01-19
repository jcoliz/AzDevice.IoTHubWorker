using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace AzDevice.Models;

public class DeviceInformationModel: IComponentModel
{
    // Manufacturer of the device THIS CODE is running on

    [JsonPropertyName("__t")]
    public string ComponentID => "c";

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("model")]
    public string? DeviceModel { get; set; }

    [JsonPropertyName("swVersion")]
    public string? SoftwareVersion { get; set; }

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

    #region IComponentModel
    string IComponentModel.dtmi => "dtmi:azure:DeviceManagement:DeviceInformation;1";

    bool IComponentModel.HasTelemetry => false;

    Task<object> IComponentModel.DoCommandAsync(string name, byte[] data)
    {
        throw new NotImplementedException();
    }

    object IComponentModel.GetProperties()
    {
        return this as DeviceInformationModel;
    }

    IDictionary<string, object> IComponentModel.GetTelemetry()
    {
        throw new NotImplementedException();
    }

    object IComponentModel.SetProperty(string key, object value)
    {
        throw new NotImplementedException();
    }
    #endregion    
}