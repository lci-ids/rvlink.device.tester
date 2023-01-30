using Xamarin.Forms;

namespace RvLinkDeviceTester.Resources.Denali
{
    public partial class Colors
    {
        public Colors()
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