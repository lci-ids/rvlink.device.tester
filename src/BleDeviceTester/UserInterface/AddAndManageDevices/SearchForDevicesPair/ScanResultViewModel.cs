using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Devices.TPMS;
using PrismExtensions.ViewModels;
using System;
using System.Linq;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices
{
    public abstract class ScanResultViewModel : Common.BaseViewModel
    {
        public DateTime LastUpdated { get; protected set; }

        public Guid Uuid { get; protected set; }

        protected ScanResultViewModel()
        {
            LastUpdated = DateTime.Now;
            _name = string.Empty;
            _description = string.Empty;
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private int _rssi;
        public int Rssi
        {
            get => _rssi;
            set => SetProperty(ref _rssi, value);
        }

        public virtual void Update(ScanResultViewModel scanResult)
        {
            LastUpdated = DateTime.Now;
            Name = scanResult.Name;
            Rssi = scanResult.Rssi;
            Description = scanResult.Description;
        }

        public override string ToString() => Name;
    }

    public class BleScanResultViewModel : ScanResultViewModel
    {
        public IBleScanResult ScanResult { get; protected set; }

        public BleScanResultViewModel(IBleScanResult scanResult)
        {


            ScanResult = scanResult;

            Name = scanResult.DeviceName;
            LastUpdated = scanResult.ScannedTimestamp;

            if (ScanResult is IBleTirePressureMonitorScanResult)
            {
                Name = scanResult.DeviceName.Contains("*")
                    ? scanResult.DeviceName.Replace("*", " ") + "(learn mode)"
                    : scanResult.DeviceName;
            }

            Rssi = scanResult.Rssi;
            Uuid = scanResult.DeviceId;

            var bytes = Uuid.ToByteArray();
            var str = BitConverter.ToString(bytes.SkipWhile((_, count) => bytes.Length - count > 6).Take(6).ToArray()).Replace("00-", "").Replace("-", ":");
            Description = $"{str}, ({Rssi}dB)";
        }

        public override void Update(ScanResultViewModel scanResult)
        {
            base.Update(scanResult);

            // Update underlying scan result object with the new one
            // 
            ScanResult = (scanResult as BleScanResultViewModel)?.ScanResult ?? ScanResult;
            LastUpdated = ScanResult.ScannedTimestamp;
        }
    }
}
