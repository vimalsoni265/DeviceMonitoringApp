# DeviceMonitoring Project

## Overview

The DeviceMonitoring project provides a robust and scalable service for managing and monitoring a collection of devices in a .NET environment. 
It allows for device registration, data polling at configurable intervals, and event-driven notifications for device state and data changes. 
The system is designed with scalability in mind, capable of efficiently handling a large number of devices.

This project is built using C# 12 and targets .NET 8.

## Core Components

*   **`DeviceMonitoringService`**: A singleton service that acts as the central hub for device management. It handles device registration, deregistration, starting/stopping monitoring, and orchestrates the device polling loop.
*   **`IDevice`**: An interface defining the contract for all devices managed by the service. It includes properties for device identification (ID, Name, Type), state, current value, and methods for performing device-specific operations and updating its state.
*   **`DeviceEntry`**: An internal class used by `DeviceMonitoringService` to manage the monitoring schedule and state for each registered device, including its polling interval, last update time, and next due time for an operation.
*   **`DeviceFactory`**: (Assumed, as `DeviceFactory.CreateDevice` is used) A factory responsible for creating instances of `IDevice` implementations.
*   **Device Implementations (e.g., `TemperatureControlDevice`)**: Concrete classes that implement `IDevice` for specific types of devices, providing logic for `PerformDeviceOperation`.
*   **Enums**:
    *   `DeviceType`: Categorizes devices (e.g., `TemperatureSensor`).
    *   `DeviceMonitorState`: Represents the operational state of a device (e.g., `Idle`, `Monitoring`, `Stopped`).
*   **Event Arguments**:
    *   `DeviceDataChangedEventArgs`: Carries data when a device's value changes.
    *   `DeviceMonitorStateChangedEventArgs`: Carries data when a device's monitoring state changes.

## Features

*   **Device Management**: Register new devices and deregister existing ones.
*   **Flexible Monitoring**: Start and stop monitoring for individual devices.
*   **Configurable Polling**: Specify custom polling intervals for each device.
*   **Asynchronous Operations**: Device operations (`PerformDeviceOperation`) are asynchronous, preventing the monitoring loop from blocking.
*   **Event-Driven Notifications**:
    *   `DeviceMonitoringService.DeviceDataChanged`: Raised when a monitored device's data is updated.
    *   `DeviceMonitoringService.DeviceMonitorStateChanged`: Raised when a device's state (e.g., started, stopped) or its internal monitor state changes.
*   *   **Singleton Service**: Ensures a single instance of `DeviceMonitoringService` throughout the application.
*   **Resource Management**: Implements `IDisposable` for proper cleanup of resources, including cancellation of monitoring tasks and disposal of device entries.

## Getting Started

### Prerequisites

*   .NET 8 SDK

### Installation

1.  Clone the repository (if applicable).
2.  Include the DeviceMonitoring project (e.g., `DeviceMonitoring.Core.csproj`, `DeviceMonitoring.Services.csproj`) in your solution.

## How to Use

Below are examples of how to interact with the `DeviceMonitoringService`.

### 1. Get the Service Instance

The `DeviceMonitoringService` is a singleton. Access it via its `Instance` property:
### 2. Register a New Device
```csharp
string deviceId = "temp-sensor-01"; string deviceName = "Living Room Temperature Sensor"; 
DeviceType deviceType = DeviceType.TemperatureSensor; 
int pollingIntervalMs = 5000; // Poll every 5 seconds
try
{ 
    service.RegisterDevice(deviceId, deviceName, deviceType, pollingIntervalMs); 
    Console.WriteLine($"Device {deviceId} registered."); 
}
catch (DeviceAlreadyExistsException ex) 
{ 
    Console.WriteLine(ex.Message); 
} 
catch (NotSupportedException ex) 
{ 
    Console.WriteLine($"Error registering device: {ex.Message}"); 
}
```

### 3. Subscribe to Events
```csharp
// Subscribe to data changes service.DeviceDataChanged += (sender, args) => { Console.WriteLine($"DataChanged: DeviceId={args.DeviceId}, NewValue={args.NewValue}"); };
// Subscribe to state changes service.DeviceMonitorStateChanged += (sender, args) => { Console.WriteLine($"StateChanged: DeviceId={args.DeviceId}, NewState={args.NewState}"); };
```
### 4. Start Monitoring a Device
```csharp
service.StartMonitoring(deviceId); 
Console.WriteLine($"Monitoring started for device {deviceId}.");
```

### 5. Stop Monitoring a Device
```csharp
service.StopMonitoring(deviceId); 
Console.WriteLine($"Monitoring stopped for device {deviceId}.");
```

### 6. Deregister a Device
```csharp
service.DeregisterDevice(deviceId); 
Console.WriteLine($"Device {deviceId} deregistered.");
```
### 9. Dispose the Service (Application Shutdown)
Ensure you dispose of the service when your application is shutting down to release resources and stop background tasks gracefully.

![image](https://github.com/user-attachments/assets/ee679ebf-2a2d-4e68-b4c8-14243fbc4c62)
![image](https://github.com/user-attachments/assets/c152a26d-64a9-4860-9cf5-d8400168ade0)

