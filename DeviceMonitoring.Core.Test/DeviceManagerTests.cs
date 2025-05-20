using DeviceMonitoring.Core.Devices;
using DeviceMonitoring.Core.Exceptions;
using DeviceMonitoring.Services;

namespace DeviceMonitoring.Core.Test
{
    [TestFixture]
    public class DeviceManagerTests : IDisposable
    {
        private DeviceMonitoringService m_deviceMonitoringService;

        [SetUp]
        public void Setup()
        {
            m_deviceMonitoringService = DeviceMonitoringService.Instance;
        }

        [Test]
        public void Constructor_InitializesDeviceManager()
        {
            // Assert
            Assert.That(m_deviceMonitoringService, Is.Not.Null, $"{nameof(DeviceMonitoringService)} is Null.");
        }

        [Test]
        public void DeviceManager_Instances_ShouldBeSameInstance()
        {
            // Arrange & Act
            var instance1 = m_deviceMonitoringService;
            var instance2 = DeviceMonitoringService.Instance;

            // Assert
            Assert.That(instance2, Is.SameAs(instance1), $"{nameof(DeviceMonitoringService)} instances are not the same.");
        }

        [Test]
        public void AddDevice_shouldThrowExceptionOnDuplicateRecordEntry()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();

            // Act - Adding first record
            m_deviceMonitoringService.RegisterDevice(deviceId, "TestDevice1", DeviceType.TemperatureSensor);

            // Assert: Adding a duplicate should throw DeviceAlreadyExistsException
            Assert.Throws<DeviceAlreadyExistsException>(() => m_deviceMonitoringService.RegisterDevice(deviceId, "TestDevice2", DeviceType.TemperatureSensor));
        }

        [Test]
        [TestCase("Id001", "TemperatureControl1", 200)]
        [TestCase("Id002", "HumiditySensor", 200)]
        [TestCase("Id003", "ComplexDevice", 200)]
        [TestCase("Id004", "FlowSensor", 200)]
        public void AddDevice_shouldManageDeviceRecords(string deviceId, string deviceName, int interval)
        {
            // Act
            m_deviceMonitoringService.RegisterDevice(deviceId, deviceName, DeviceType.TemperatureSensor);
            var getDevice = m_deviceMonitoringService.GetDevice<TemperatureControlDevice>(deviceId);

            // Assert
            Assert.That(getDevice, Is.Not.Null, $"{deviceName} is not found in the device list.");
            Assert.That(getDevice.Id, Is.EqualTo(deviceId), $"{deviceName} ID does not match.");
        }

        [Test]
        public void GetDevice_ShouldReturnCorrectDevice()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            m_deviceMonitoringService.RegisterDevice(deviceId, "TestDevice1", DeviceType.TemperatureSensor);

            // Act
            var retrievedDevice = m_deviceMonitoringService.GetDevice<TemperatureControlDevice>(deviceId);

            // Assert
            Assert.That(retrievedDevice, Is.Not.Null, "The retrieved device is null.");
            Assert.That(retrievedDevice.Id, Is.EqualTo(deviceId), "The retrieved device ID does not match.");
            Assert.That(retrievedDevice.DeviceMonitorState, Is.EqualTo(DeviceMonitorState.Idle), "The retrieved device should be set to Idle state by default.");
        }

        [Test]
        [TestCase("Test Device", DeviceType.TemperatureSensor)]
        public void StartMonitoring_ShouldSetDeviceStateToMonitoring_AndRaiseEvent(string deviceName, DeviceType type)
        {
            // Arrange
            string? eventDeviceId = null;
            DeviceMonitorState? eventState = null;
            string deviceId = Guid.NewGuid().ToString();

            m_deviceMonitoringService.DeviceMonitorStateChanged += (obj, args) =>
            {
                eventDeviceId = args.DeviceId;
                eventState = args.NewState;
            };

            m_deviceMonitoringService.RegisterDevice(deviceId, deviceName, type);

            // Act
            m_deviceMonitoringService.StartMonitoring<TemperatureControlDevice>(deviceId);
            var device = m_deviceMonitoringService.GetDevice<TemperatureControlDevice>(deviceId);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(device.DeviceMonitorState, Is.EqualTo(DeviceMonitorState.Monitoring), "Device state is not set to Monitoring.");
                Assert.That(eventDeviceId, Is.EqualTo(deviceId), "Event device ID does not match.");
                Assert.That(eventState, Is.EqualTo(DeviceMonitorState.Monitoring), "Event state is not Monitoring.");
            });
        }

        [Test]
        public void Stopping_ShouldSetDeviceStateToStopping_AndRaiseEvent()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var device = new TemperatureControlDevice(deviceId, "Test Device");

            string? eventDeviceId = null;
            DeviceMonitorState? eventState = null;

            m_deviceMonitoringService.DeviceMonitorStateChanged += (obj, args) =>
            {
                eventDeviceId = args.DeviceId;
                eventState = args.NewState;
            };

            // Act
            m_deviceMonitoringService.RegisterDevice(deviceId, "Test Device", DeviceType.TemperatureSensor);
            m_deviceMonitoringService.StartMonitoring<IDevice>(deviceId);
            Thread.Sleep(1000);
            m_deviceMonitoringService.StopMonitoring<IDevice>(deviceId);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(device.DeviceMonitorState, Is.EqualTo(DeviceMonitorState.Idle), "Device state is not set to Stopped.");
                Assert.That(eventDeviceId, Is.EqualTo(deviceId), "Event device ID does not match.");
                Assert.That(eventState, Is.EqualTo(DeviceMonitorState.Stopped), "Event state is not Stopped.");
            });
        }

        [Test]
        public void Stopping_ShouldHandledAlreadyStoppedDevices()
        {
            // Arrange
            string? eventDeviceId = null;
            DeviceMonitorState? eventState = null;
            string deviceId = Guid.NewGuid().ToString();    

            m_deviceMonitoringService.RegisterDevice(deviceId, "TestTempDevice", DeviceType.TemperatureSensor);

            m_deviceMonitoringService.DeviceMonitorStateChanged += (_, args) =>
            {
                eventDeviceId = args.DeviceId;
                eventState = args.NewState;
            };

            var device = m_deviceMonitoringService.GetDevice<TemperatureControlDevice>(deviceId);

            // Act
            m_deviceMonitoringService.StopMonitoring<TemperatureControlDevice>(deviceId);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(deviceId, Is.EqualTo(eventDeviceId));
                Assert.That(eventState, Is.EqualTo(DeviceMonitorState.Stopped));
                Assert.That(device.DeviceMonitorState, Is.EqualTo(DeviceMonitorState.Stopped));
            });
        }

        [Test]
        public void StopMonitoring_ShouldRaiseEventWithCorrectDeviceId()
        {
            // Arrange
            m_deviceMonitoringService.RegisterDevice("devId007", "TempDev007", DeviceType.TemperatureSensor);
            m_deviceMonitoringService.RegisterDevice("devId009", "TempDev009", DeviceType.TemperatureSensor);

            string? eventId = null;
            m_deviceMonitoringService.DeviceMonitorStateChanged += (_, args) => eventId = args.DeviceId;

            // Act
            m_deviceMonitoringService.StopMonitoring<TemperatureControlDevice>("devId007");

            // Assert
            Assert.That(eventId, Is.EqualTo("devId007"));
        }

        [Test]
        public void StopMonitoring_ShouldNotRaiseEventForNullOrEmptyDeviceId()
        {
            // Arrange
            bool eventRaised = false;
            m_deviceMonitoringService.DeviceMonitorStateChanged += (_, _) => eventRaised = true;

            // Act
            m_deviceMonitoringService.StopMonitoring<IDevice>(null!);
            m_deviceMonitoringService.StopMonitoring<IDevice>(string.Empty);

            // Assert
            Assert.That(eventRaised, Is.False, $"MonitoringStatusChanged should not throw exception and should not raise any event" );
        }

        public void Dispose()
        {
            m_deviceMonitoringService.Dispose();
        }
    }
}