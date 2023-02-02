using System;
using System.Collections.Generic;

namespace RvLinkDeviceTester
{
    public interface IAppSettingsMacHistory
    {
        List<string> MacHistory { get; }
        void AddToMacHistory(string mac, bool autoSave = true);
        void SetMacHistory(List<string> macHistory);
        void SetNumMacsSaved(int numMacsSaved);
    }

    public partial class AppSettings
    {
        private List<string> _macHistory = new List<string>();
        public List<string> MacHistory => _macHistory;

        public void AddToMacHistory(string mac, bool autoSave = true)
        {
            if (_macHistory.Contains(mac))
                return;

            _macHistory.Add(mac);

            if (_macHistory.Count > _numMacsSaved)
            {
                _macHistory.RemoveAt(_numMacsSaved);
            }

            if (autoSave)
                Save();
        }

        public void SetMacHistory(List<string> macHistory)
        {
            foreach (string mac in macHistory)
            {
                AddToMacHistory(mac, false);
            }
        }

        private int _numMacsSaved = 20;
        public void SetNumMacsSaved(int numMacsSaved)
        {
            _numMacsSaved = numMacsSaved;
        }
    }
}

