using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ClipFlow.Controls
{
    public partial class CardExpander : UserControl
    {
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<CardExpander, string>(nameof(Header));

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<CardExpander, bool>(nameof(IsExpanded), defaultValue: true);

        public static readonly StyledProperty<object> CardContentProperty =
            AvaloniaProperty.Register<CardExpander, object>(nameof(CardContent));

        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public object CardContent
        {
            get => GetValue(CardContentProperty);
            set => SetValue(CardContentProperty, value);
        }

        public CardExpander()
        {
            InitializeComponent();
        }

        private void OnHeaderTapped(object? sender, TappedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }
    }
} 