using Avalonia;
using Avalonia.Controls;
using System;

namespace ClipFlow.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Opened += OnOpened;
        }
        private void OnOpened(object? sender, EventArgs e)
        {
            ApplyPlatformChrome();
        }

        private void ApplyPlatformChrome()
        {
            //if (OperatingSystem.IsMacOS())
            //{
            //    ExtendClientAreaToDecorationsHint = false;
            //}
            //else
            //{
            //    ExtendClientAreaToDecorationsHint = true;
            //    ExtendClientAreaTitleBarHeightHint = 70;
            //}
            //ExtendClientAreaToDecorationsHint = false;
        }

    }
}