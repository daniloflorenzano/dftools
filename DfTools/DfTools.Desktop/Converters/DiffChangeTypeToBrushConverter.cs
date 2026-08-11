using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DfTools.Diff;

namespace DfTools.Desktop.Converters;

public class DiffChangeTypeToBrushConverter : IValueConverter
{
    public static readonly DiffChangeTypeToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DiffChangeType changeType)
        {
            var isBackground = parameter?.ToString()?.Equals("Background", StringComparison.OrdinalIgnoreCase) ?? true;

            return changeType switch
            {
                DiffChangeType.Inserted => Brush.Parse(isBackground ? "#1e3a1e" : "#4caf50"),
                DiffChangeType.Deleted => Brush.Parse(isBackground ? "#3a1e1e" : "#f44336"),
                DiffChangeType.Modified => Brush.Parse(isBackground ? "#3a331e" : "#ffb74d"),
                DiffChangeType.Imaginary => Brush.Parse(isBackground ? "#151515" : "#444444"),
                _ => Brush.Parse(isBackground ? "Transparent" : "#FFFFFF")
            };
        }
        return Brush.Parse("Transparent");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DiffSubPieceBrushConverter : IValueConverter
{
    public static readonly DiffSubPieceBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DiffChangeType changeType)
        {
            var paramStr = parameter?.ToString();
            var isBackground = paramStr?.Equals("Background", StringComparison.OrdinalIgnoreCase) ?? true;
            var isBorder = paramStr?.Equals("Border", StringComparison.OrdinalIgnoreCase) ?? false;

            if (isBorder)
            {
                return changeType switch
                {
                    DiffChangeType.Inserted => Brush.Parse("#4caf50"),
                    DiffChangeType.Deleted => Brush.Parse("#f44336"),
                    DiffChangeType.Modified => Brush.Parse("#ffb74d"),
                    _ => Brush.Parse("Transparent")
                };
            }

            return changeType switch
            {
                DiffChangeType.Inserted => Brush.Parse(isBackground ? "#2e6f30" : "#a5d6a7"),
                DiffChangeType.Deleted => Brush.Parse(isBackground ? "#6f2e2e" : "#ef9a9a"),
                DiffChangeType.Modified => Brush.Parse(isBackground ? "#5a4d1e" : "#ffe082"),
                _ => Brush.Parse(isBackground ? "Transparent" : "#FFFFFF")
            };
        }
        return Brush.Parse("Transparent");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

