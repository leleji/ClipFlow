using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;


namespace ClipFlow.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateWindowBackground();
           

        }
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            Application.Current!.ActualThemeVariantChanged += OnThemeChanged;
            UpdateWindowBackground();
        }

        protected override void OnClosed(EventArgs e)
        {
            Application.Current!.ActualThemeVariantChanged -= OnThemeChanged;
            base.OnClosed(e);
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            UpdateWindowBackground();
        }
        private void UpdateWindowBackground()
        {
            var level = ActualTransparencyLevel;
            var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            if (level == WindowTransparencyLevel.Mica)
            {
                Background = Brushes.Transparent;
               
            }
            else if(level == WindowTransparencyLevel.AcrylicBlur)
            {
                Background = new SolidColorBrush(
                   Color.Parse(isDark ? "#CC1E1E1E" : "#CCF3F3F3"));
            }
            else
            {
                Background = new SolidColorBrush(
                    Color.Parse(isDark ? "#FF1E1E1E" : "#FFFFFFFF"));
            }
        }

    }
}