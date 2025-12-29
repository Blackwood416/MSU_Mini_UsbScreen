using System.IO;
using System.Reflection;

namespace UsbScreen.Utils;

public static class FontHelper
{
    public static Stream? GetDefaultFontStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream("UsbScreen.Core.Fonts.SourceHanSansSC-Normal-Min.ttf");
    }
}
