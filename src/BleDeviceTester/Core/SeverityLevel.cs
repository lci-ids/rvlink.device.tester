using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

namespace RvLinkDeviceTester
{
    /// <summary>
    /// Syncfusion's Combobox doesn't work well when binding directly to a list of strings, it works better with objects.
    /// </summary>
    public class SeverityLevel : BindableBase
    {
        private string? severity;
        public string? Severity { get => severity; set => SetProperty(ref severity, value); }
    }
}
