using Xamarin.Forms;

// Due to a bug in Xamarin Forms, we can rename this font file to ensure
// that new glyphs appear, otherwise previous app installations may used a cached version of the .ttf file.
// See https://github.com/xamarin/Xamarin.Forms/issues/11843 for more info.
[assembly: ExportFont("ocm_4.ttf", Alias = "ocm")]