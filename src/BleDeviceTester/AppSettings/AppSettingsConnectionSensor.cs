using System;
using IDS.Core.Collections;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.AwningSensor;
using OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.TemperatureSensor;
using System.Collections.Generic;
using System.Linq;
using RvLinkDeviceTester.Connections.Sensors;
using OneControl.Direct.IdsCanAccessoryBle.GenericSensor;
using RvLinkDeviceTester.Services;
using OneControl.Devices.TankSensor.Mopeka;

namespace RvLinkDeviceTester
{
    public interface IAppSettingsConnectionSensor
    {
        IReadOnlyList<ILogicalDeviceSourceDirect> StandardSharedSensorSources { get; }

        public IEnumerable<ISensorConnection> SensorConnectionsAll { get; }

        bool HasSensorConnection { get; }

        bool IsSensorConnectionKnown(ISensorConnection sensorConnection);
        
        bool TryAddSensorConnection(ISensorConnection sensorConnection, bool autoSave = true);

        bool RemoveSensorConnection(ISensorConnection sensorConnection, bool autoSave = true);

        void SetSensorConnections(IReadOnlyList<ISensorConnection> sensorConnections, bool autoSave, bool notifyChanged);
    }

    public partial class AppSettings : IAppSettingsConnectionSensor
    {
        private readonly ConcurrentHashSet<SensorConnectionTemperature> _temperatureSensorConnections = new ConcurrentHashSet<SensorConnectionTemperature>();
        private readonly ConcurrentHashSet<SensorConnectionBatteryMonitor> _batteryMonitorSensorConnections = new ConcurrentHashSet<SensorConnectionBatteryMonitor>();
        private readonly ConcurrentHashSet<SensorConnectionAwningSensor> _awningSensorConnections = new ConcurrentHashSet<SensorConnectionAwningSensor>();
        private readonly ConcurrentHashSet<SensorConnectionMopeka> _mopekaSensorConnections = new ConcurrentHashSet<SensorConnectionMopeka>();
        private readonly ConcurrentHashSet<SensorConnectionGeneric> _genericSensorConnections = new ConcurrentHashSet<SensorConnectionGeneric>();

        #region Standard Sensor Device Sources
        private IReadOnlyList<ILogicalDeviceSourceDirect>? _standardSharedSensorSources;
        public IReadOnlyList<ILogicalDeviceSourceDirect> StandardSharedSensorSources
        {
            get
            {
                if( _standardSharedSensorSources != null )
                    return _standardSharedSensorSources;

                var standardSensorSources = new List<ILogicalDeviceSourceDirect>();

                var temperatureDeviceSource = Resolver<IDirectTemperatureSensorBle>.Resolve;
                if( temperatureDeviceSource is not null )
                    standardSensorSources.Add(temperatureDeviceSource);

                var batteryMonitorDeviceSource = Resolver<IDirectBatteryMonitorBle>.Resolve;
                if (batteryMonitorDeviceSource is not null)
                    standardSensorSources.Add(batteryMonitorDeviceSource);

                var mopekaDeviceSource = MopekaBleDeviceSource.SharedExtension;
                if( mopekaDeviceSource != null )
                    standardSensorSources.Add(mopekaDeviceSource);

                var genericSensorDeviceSource = Resolver<IDirectGenericSensorBle>.Resolve;
                if (genericSensorDeviceSource is not null)
                    standardSensorSources.Add(genericSensorDeviceSource);

                _standardSharedSensorSources = standardSensorSources;
                return _standardSharedSensorSources;
            }
        }
        #endregion

        public IEnumerable<ISensorConnection> SensorConnectionsAll
        {
            get
            {
                foreach( var sensor in _temperatureSensorConnections )
                    yield return sensor;

                foreach (var sensor in _batteryMonitorSensorConnections)
                    yield return sensor;

                foreach (var sensor in _awningSensorConnections)
                    yield return sensor;

                foreach ( var sensor in _mopekaSensorConnections )
                    yield return sensor;

                foreach (var sensor in _genericSensorConnections)
                    yield return sensor;
            }
        }

        public IEnumerable<TSensorConnection> SensorConnections<TSensorConnection>()
            where TSensorConnection : ISensorConnection => SensorConnectionsAll.OfType<TSensorConnection>();

        public bool HasSensorConnection => SensorConnectionsAll.Any();

        public bool IsSensorConnectionKnown(ISensorConnection sensorConnection) => SensorConnectionsAll.Contains(sensorConnection);
        
        /// <summary>
        /// </summary>
        /// <param name="sensorConnection">A supported sensor connection</param>
        /// <param name="autoSave">If true will trigger a save is the sensor was newly added to the list</param>
        /// <returns>true if the sensor was added, or false if the sensor was already added and/or is not able to be added</returns>
        public bool TryAddSensorConnection(ISensorConnection sensorConnection, bool autoSave = true)
        {
            switch( sensorConnection )
            {
                case SensorConnectionTemperature connection: 
                    if( _temperatureSensorConnections.Contains(connection) )
                        return false;

                    Resolver<IDirectTemperatureSensorBle>.Resolve?.RegisterTemperatureSensor(connection.ConnectionGuid, connection.AccessoryMac, connection.SoftwarePartNumber, connection.ConnectionNameFriendly);
                    _temperatureSensorConnections.Add(connection);
                    break;

                case SensorConnectionBatteryMonitor connection:
                    if (_batteryMonitorSensorConnections.Contains(connection))
                        return false;

                    Resolver<IDirectBatteryMonitorBle>.Resolve?.RegisterBatteryMonitor(connection.ConnectionGuid, connection.AccessoryMac, connection.SoftwarePartNumber, connection.ConnectionNameFriendly);
                    _batteryMonitorSensorConnections.Add(connection);
                    break;

                case SensorConnectionAwningSensor connection:
                    if (_awningSensorConnections.Contains(connection))
                        return false;

                    Resolver<IDirectAwningSensorBle>.Resolve?.RegisterAwningSensor(connection.ConnectionGuid, connection.AccessoryMac, connection.SoftwarePartNumber, connection.ConnectionNameFriendly);
                    _awningSensorConnections.Add(connection);
                    break;

                case SensorConnectionMopeka connection:
                    if( _mopekaSensorConnections.Contains(connection) )
                        return false;

                    var mopekaConnection = new OneControl.Direct.IdsCanAccessoryBle.Connections.SensorConnectionMopeka(connection.ConnectionNameFriendly,
                        connection.ConnectionGuid, connection.MacAddress,
                        connection.DefaultFunctionName,
                        connection.DefaultFunctionInstance,
                        connection.DefaultTankSizeId,
                        connection.DefaultTankHeightInMm,
                        connection.DefaultIsNotificationEnabled,
                        connection.DefaultNotificationThreshold,
                        defaultPreferredUnits: connection.DefaultPreferredUnits);
                    MopekaBleDeviceSource.SharedExtension?.LinkMopekaSensor(mopekaConnection);
                    _mopekaSensorConnections.Add(connection);
                    break;

                case SensorConnectionGeneric connection:
                    if (_genericSensorConnections.Contains(connection))
                        return false;

                    Resolver<IDirectGenericSensorBle>.Resolve?.RegisterGenericSensor(connection.ConnectionGuid, connection.AccessoryMac, connection.SoftwarePartNumber, connection.ConnectionNameFriendly);
                    _genericSensorConnections.Add(connection);
                    break;

                default:
                    TaggedLog.Warning(LogTag, $"Sensor can't be added because it's unknown {sensorConnection}");
                    return false;
            }

            if( autoSave ) {
                Save();

                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="sensorConnection">A supported sensor connection</param>
        /// <param name="autoSave">If true will trigger a save is the sensor was removed from the list</param>
        /// <returns>true if the sensor was removed and unregistered, otherwise returns false</returns>
        public bool RemoveSensorConnection(ISensorConnection sensorConnection, bool autoSave = true)
        {
            switch( sensorConnection )
            {
                case SensorConnectionTemperature connection:
                    if( !_temperatureSensorConnections.Contains(connection) )
                        return false;

                    Resolver<IDirectTemperatureSensorBle>.Resolve?.UnRegisterTemperatureSensor(connection.ConnectionGuid);
                    _temperatureSensorConnections.TryRemove(connection);
                    break;

                case SensorConnectionBatteryMonitor connection:
                    if (!_batteryMonitorSensorConnections.Contains(connection))
                        return false;

                    Resolver<IDirectBatteryMonitorBle>.Resolve?.UnRegisterBatteryMonitor(connection.ConnectionGuid);
                    _batteryMonitorSensorConnections.TryRemove(connection);
                    break;

                case SensorConnectionMopeka connection:
                    if( !_mopekaSensorConnections.Contains(connection) )
                        return false;

                    MopekaBleDeviceSource.SharedExtension?.UnlinkMopekaSensor(connection.MacAddress);
                    _mopekaSensorConnections.TryRemove(connection);
                    break;

                case SensorConnectionGeneric connection:
                    if (!_genericSensorConnections.Contains(connection))
                        return false;

                    Resolver<IDirectGenericSensorBle>.Resolve?.UnRegisterGenericSensor(connection.ConnectionGuid);
                    _genericSensorConnections.TryRemove(connection);
                    break;

                default:
                    TaggedLog.Warning(LogTag, $"Sensor can't be added because it's unknown {sensorConnection}");
                    return false;
            }

            if (autoSave) {
                Save();

                // Persists the removal of the device source from the logical device.
                AppSettings.Instance.TakeSnapshot();

                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="sensorConnections"></param>
        /// <param name="autoSave">Iff true and there was a modification to the sensors added or removed a save will be performed</param>
        public void SetSensorConnections(IReadOnlyList<ISensorConnection> sensorConnections, bool autoSave, bool notifyChanged)
        {
            int numModification = 0;

            // Remove connections that are in the given sensorConnectionList
            //
            foreach( var connection in _temperatureSensorConnections.ToList().Where(connection => !sensorConnections.Contains(connection)) )
            {
                numModification += 1;
                RemoveSensorConnection(connection, autoSave: false);
            }

            foreach (var connection in _batteryMonitorSensorConnections.ToList().Where(connection => !sensorConnections.Contains(connection)))
            {
                numModification += 1;
                RemoveSensorConnection(connection, autoSave: false);
            }

            foreach ( var connection in _mopekaSensorConnections.ToList().Where(connection => !sensorConnections.Contains(connection)) )
            {
                numModification += 1;
                RemoveSensorConnection(connection, autoSave: false);
            }

            foreach (var connection in _genericSensorConnections.ToList().Where(connection => !sensorConnections.Contains(connection)))
            {
                numModification += 1;
                RemoveSensorConnection(connection, autoSave: false);
            }

            // Add the connections if they aren't already in the list
            //
            foreach ( var connection in sensorConnections )
                numModification += TryAddSensorConnection(connection, autoSave: false) ? 1 : 0;

            AppDirectConnectionService.Instance.Start();

            if ( autoSave && numModification > 0 )
                Save();

            // Sensors don't have their own update notification they just piggy back on the AppSelectedRvUpdateMessage
            //
            if( notifyChanged && numModification > 0 )
                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
        }
    }
}
