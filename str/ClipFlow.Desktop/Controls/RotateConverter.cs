using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ClipFlow.Desktop.Controls
{
    public class RotateConverter : IValueConverter
    {
        public static readonly RotateConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool isExpanded && isExpanded ? "rotate(180deg)" : "rotate(0deg)";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}