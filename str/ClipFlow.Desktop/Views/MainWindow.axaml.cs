using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.IO;
using ClipFlow.Desktop.ViewModels;
using Avalonia.Controls.ApplicationLifetimes;
using ClipFlow.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClipFlow.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

    }
}