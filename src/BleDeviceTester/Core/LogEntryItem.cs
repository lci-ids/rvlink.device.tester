using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RvLinkDeviceTester
{
    public interface ILogEntry
    {
        string? DeviceName { get; set; }
        DateTimeOffset LoggedOn { get; set; }
        string? Message { get; set; }
        string? Severity { get; set; }
    }

    /// <summary>
    /// Placeholder ViewModel representing one entry in the log file.
    /// </summary>
    public class LogEntryItem : BindableBase, ILogEntry
    {
        private DateTimeOffset loggedOn;
        public DateTimeOffset LoggedOn { get => loggedOn; set => SetProperty(ref loggedOn, value); }

        private string? deviceName;
        public string? DeviceName { get => deviceName; set => SetProperty(ref deviceName, value); }

        private string? severity;
        public string? Severity { get => severity; set => SetProperty(ref severity, value); }

        private string? message;
        public string? Message { get => message; set => SetProperty(ref message, value); }
    }
}
