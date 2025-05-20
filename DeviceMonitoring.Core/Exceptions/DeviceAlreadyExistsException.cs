namespace DeviceMonitoring.Core.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when attempting to add a device with an ID that already exists.
    /// </summary>
    /// <remarks>This exception is typically thrown when a device with the specified ID is already present in
    /// a collection or system that enforces unique device identifiers.</remarks>
    /// <param name="deviceId">The ID of the device that caused the exception.</param>
    public class DeviceAlreadyExistsException(string deviceId)
        : Exception($"A device with ID {deviceId} already exists.")
    {
        public string DeviceId { get; } = deviceId;
    }
}
