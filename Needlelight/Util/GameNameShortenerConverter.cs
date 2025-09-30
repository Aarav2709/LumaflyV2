using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Needlelight.Util;

/// <summary>
/// Converts long-form game names into compact labels suitable for UI chrome.
/// </summary>
public sealed class GameNameShortenerConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name)
        {
            return AvaloniaProperty.UnsetValue;
        }

        return name switch
        {
            "Hollow Knight Silksong" => "HK : Silksong",
            _ => name
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}
