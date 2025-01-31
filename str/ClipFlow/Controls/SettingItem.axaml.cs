using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Markup.Xaml;

namespace ClipFlow.Desktop.Controls
{
    public partial class SettingItem : UserControl
    {
        public static readonly StyledProperty<IBrush> BackgroundColorProperty =
            AvaloniaProperty.Register<SettingItem, IBrush>(nameof(BackgroundColor));

        public static readonly StyledProperty<Thickness> BorderThicknessProperty =
            AvaloniaProperty.Register<SettingItem, Thickness>(nameof(BorderThickness), new Thickness(1));

        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<SettingItem, CornerRadius>(nameof(CornerRadius), new CornerRadius(4));

        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<SettingItem, string>(nameof(Title));

        public static readonly StyledProperty<string> DescriptionProperty =
            AvaloniaProperty.Register<SettingItem, string>(nameof(Description));

        public static readonly AttachedProperty<object> ActionContentProperty =
            AvaloniaProperty.RegisterAttached<SettingItem, Control, object>
            (
                "ActionContent",
                null,
                false,
                Avalonia.Data.BindingMode.TwoWay
            );

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public IBrush BackgroundColor
        {
            get => GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        public Thickness BorderThickness
        {
            get => GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public object ActionContent
        {
            get => GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }

        public static void SetActionContent(Control element, object value)
        {
            element.SetValue(ActionContentProperty, value);
        }

        public static object GetActionContent(Control element)
        {
            return element.GetValue(ActionContentProperty);
        }

        public SettingItem()
        {
            InitializeComponent();
        }
    }
} 