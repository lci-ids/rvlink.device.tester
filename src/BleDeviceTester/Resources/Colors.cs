#nullable enable

using System;
using Xamarin.Forms;
using Microsoft.AppCenter.Crashes;

namespace RvLinkDeviceTester.Resources
{
    public static class Colors
    {
        public static Color TransparentBlack => ColorFromResource("TransparentBlack");
        public static Color TransparentWhite => ColorFromResource("TransparentWhite");
        public static Color SemitransparentWhite => ColorFromResource("SemitransparentWhite");
        public static Color OffWhite => ColorFromResource("OffWhite");
        public static Color AquaMarine => ColorFromResource("AquaMarine");
        public static Color RaincoatYellow => ColorFromResource("RaincoatYellow");
        public static Color PastelOrange => ColorFromResource("PastelOrange");
        public static Color LightBlue => ColorFromResource("LightBlue");
        public static Color Blue => ColorFromResource("Blue");
        public static Color TwilightBlue => ColorFromResource("TwilightBlue");
        public static Color Black => ColorFromResource("Black");
        public static Color DullRed => ColorFromResource("DullRed");
        public static Color DullRedTransparent => ColorFromResource("DullRedTransparent");
        public static Color TextGray => ColorFromResource("TextGray");
        public static Color TextColor => ColorFromResource("TextColor");
        public static Color BubbleYellow => ColorFromResource("BubbleYellow");
        public static Color BubbleGrey => ColorFromResource("BubbleGrey");
        public static Color BackgroundImageTint => ColorFromResource("BackgroundImageTint");
        public static Color PrimaryColor => ColorFromResource("PrimaryColor");

        public static Color SurfaceColor => ColorFromResource("SurfaceColor");
       public static Color TransparentPrimary => ColorFromResource("PrimaryColor").WithAlpha(0.40);
        public static Color SecondaryColor => ColorFromResource("SecondaryColor");
        public static Color TertiaryColor => ColorFromResource("TertiaryColor");
        public static Color ButtonColor => ColorFromResource("ButtonColor");
        public static Color RVHealthButtonColor => ColorFromResource("RVHealthButtonColor");
        public static Color TextColorOnPrimary => ColorFromResource("TextColorOnPrimary");
        public static Color PageBackgroundColor => ColorFromResource("PageBackgroundColor");
        public static Color[] WhiteGradient { get; internal set; } = {
            Color.FromHex("#00FFFFFF"),
            Color.FromHex("#16FFFFFF"),
            Color.FromHex("#48FFFFFF")
        };

        public static Color[] SemitransparentBlackGradient { get; internal set; } = {
            BackgroundImageTint,
            BackgroundImageTint,
            Color.FromHex("#48000000")
        };

        private static Color ColorFromResource(string colorName)
        {
            try
            {
                if (Application.Current == null) return Color.Transparent; //Called too early Forms app is not setup yet.
                if (!Application.Current.Resources.TryGetValue(colorName, out var output))
                    throw new ColorException($"Resource {colorName} was not found!");
                if (output is Color color)
                {
                    return color;
                }
                return Color.Transparent;
            }
            catch (Exception)
            {
                throw new ColorException($"Resource {colorName} is not a color!");

#if DEBUG
#else

                //If color is not found log the error and return Transparent so we don't cash
                Crashes.TrackError(new Exception($"Resource {colorName} was not found!"));
                return Color.Transparent;
#endif
            }
        }
        
        public static string GetRgbFill(Color color)
        {
            var red = (int)(color.R * 255);
            var green = (int)(color.G * 255);
            var blue = (int)(color.B * 255);
            var rgbFill = $"fill: rgb({red},{green},{blue});";
            return rgbFill;
        }

#region Extension Methods
        public static Color ToDarkerShade(this Color color)
            => Color.FromHsla(color.Hue, color.Saturation, color.Luminosity * 0.5, color.A);

        public static Color WithAlpha(this Color color, double alpha)
            => Color.FromHsla(color.Hue, color.Saturation, color.Luminosity, alpha);
#endregion
    }

    public class ColorException : Exception
    {
        public ColorException(string message) : base(message)
        {
            
        }
    }
}