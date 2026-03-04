using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ClipFlow.Core.Services;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using Avalonia;
using ClipFlow.Common.Helpers;
using ClipFlow.Common.Models;

namespace ClipFlow.Desktop.Views
{
    public partial class LogPage : UserControl
    {
        private Border? _lastSelectedBorder;

        public LogPage()
        {
            InitializeComponent();

            AttachedToVisualTree += OnAttachedToVisualTree;
            DetachedFromVisualTree += OnDetachedFromVisualTree;
        }


        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            LogService.Instance.LogAdded += OnLogAdded;
            ScrollToBottom();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            LogService.Instance.LogAdded -= OnLogAdded;
        }

        private void OnLogAdded(object? sender, EventArgs e)
        {
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Dispatcher.UIThread.Post(
                () => LogScrollViewer?.ScrollToEnd(),
                DispatcherPriority.Background);
        }

        private void OnLogItemPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border currentBorder && currentBorder.DataContext is LogItem logItem)
            {
                // 恢复上一个选中项的背景色
                if (_lastSelectedBorder != null)
                {
                    _lastSelectedBorder.Background = new SolidColorBrush(Colors.Transparent);
                }

                // 设置当前选中项的背景色
                currentBorder.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                _lastSelectedBorder = currentBorder;

                // 更新 ViewModel 中的选中项
                if (DataContext is Core.ViewModels.LogViewModel vm)
                {
                    vm.SelectedLogItem = logItem;
                }
            }
        }
    }
} 