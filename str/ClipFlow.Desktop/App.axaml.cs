using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ClipFlow.Desktop.Services;
using ClipFlow.Desktop.Views;
using System.Threading.Tasks;
using System;

namespace ClipFlow.Desktop
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // 加载配置并设置主题
            var themeMode = ConfigService.Instance.CurrentConfig.ThemeMode;
            RequestedThemeVariant = themeMode switch
            {
                0 => null, // 跟随系统
                1 => ThemeVariant.Light,
                2 => ThemeVariant.Dark,
                _ => null
            };

        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 设置全局异常处理
            GlobalExceptionHandler.Setup();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}