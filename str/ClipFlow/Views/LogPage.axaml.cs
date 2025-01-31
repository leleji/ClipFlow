using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ClipFlow.Desktop.Services;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using Avalonia;

namespace ClipFlow.Desktop.Views
{
    public partial class LogPage : UserControl
    {
        private Border? _lastSelectedBorder;

        public LogPage()
        {
            InitializeComponent();

            // 订阅日志添加事件
            LogService.Instance.LogAdded += (s, e) =>
            {
                ScrollToBottom();
            };

            // 订阅页面加载事件
            this.AttachedToVisualTree += (s, e) =>
            {
                ScrollToBottom();
            };
        }

        private void ScrollToBottom()
        {
            Dispatcher.UIThread.Post(async () =>
            {
                // 等待一帧以确保布局已更新
                await Task.Delay(10);
                LogScrollViewer?.ScrollToEnd();
            });
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
                if (DataContext is Desktop.ViewModels.LogViewModel vm)
                {
                    vm.SelectedLogItem = logItem;
                }
            }
        }
    }
} 