using Xamarin.Forms;

namespace RvLinkDeviceTester.Resources
{
    public partial class DarkColors
    {
        public DarkColors()
        {
            InitializeComponent();
        }

        public Color PrimaryColor
        {
            get
            {
                object color;
                if (TryGetValue("PrimaryColor", out color ))
                    return (color is Color ? (Color)color : default);

                return default;
            }
        }
        
        public Color AccentColor
        {
            get
            {
                object color;
                if (TryGetValue("AccentColor", out color ))
                    return (color is Color ? (Color)color : default);

                return default;
            }
        }
        
        public Color SecondaryColor
        {
            get
            {
                object color;
                if (TryGetValue("SecondaryColor", out color ))
                    return (color is Color ? (Color)color : default);

                return default;
            }
        }
    }
}