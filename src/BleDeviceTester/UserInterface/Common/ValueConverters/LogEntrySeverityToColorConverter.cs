using RvLinkDeviceTester.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface.Common.ValueConverters
{
    public class LogEntrySeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var severity = value?.ToString() ?? string.Empty;

            // We can move the color values into a resource dictionary at a later time
            // if we have a need to let the user customize it.

            if (severity.Equals(Strings.log_severity_information, StringComparison.OrdinalIgnoreCase))
                return Color.White;
            else if (severity.Equals(Strings.log_severity_debug, StringComparison.OrdinalIgnoreCase))
                return Color.LightGray;
            else if (severity.Equals(Strings.log_severity_error, StringComparison.OrdinalIgnoreCase))
                return Color.Red;
            else if (severity.Equals(Strings.log_severity_warning, StringComparison.OrdinalIgnoreCase))
                return Color.Orange;
            

            return Color.White;            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
