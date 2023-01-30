using System;
using IDS.Portable.BLE.Platforms.Shared.BleManager;

namespace RvLinkDeviceTester
{
    public interface IAppSettingsBle
    {
        bool UseKeySeedExchange { get; }
        void SetUseKeySeedExchange(bool useKeySeedExchange, bool autoSave = true);
        void SetGenerateNotifications(bool generateNotifications, bool autoSave = true);
    }

    public partial class AppSettings
    {
        private bool _useKeySeedExchange = false;
        public bool UseKeySeedExchange => _useKeySeedExchange;

        public void SetUseKeySeedExchange(bool useKeySeedExchange, bool autoSave = true)
        {
            if (_useKeySeedExchange == useKeySeedExchange)
                return;

            _useKeySeedExchange = useKeySeedExchange;

            BleManager.Instance.SetUseKeySeed(useKeySeedExchange);

            if (autoSave)
                Save();
        }

        private bool _generateNotifications = false;
        public bool GenerateNotifications => _generateNotifications;
        public void SetGenerateNotifications(bool generateNotifications, bool autoSave = true)
        {
            if (_generateNotifications == generateNotifications)
                return;

            _generateNotifications = generateNotifications;

            if (autoSave)
                Save();
        }
    }
}
