using Avalonia;
using Avalonia.Media;
using System;

namespace ClipFlow
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .With(new FontManagerOptions
                {
                    DefaultFamilyName = "avares://Avalonia.Fonts.Inter/Assets#Inter",
                    FontFallbacks = new[]
                    {
                        new FontFallback { FontFamily = "Microsoft YaHei UI" },
                        new FontFallback { FontFamily = "Noto Sans CJK SC" },
                        new FontFallback { FontFamily = "PingFang SC" },
                        new FontFallback { FontFamily = "Source Han Sans SC" },
                        new FontFallback { FontFamily = "WenQuanYi Micro Hei" }
                    }
                });
    }
}
