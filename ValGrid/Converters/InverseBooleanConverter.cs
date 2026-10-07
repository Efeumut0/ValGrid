using System;
using System.Globalization;
using System.Windows.Data;

namespace ValGrid.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBooleanConverter : IValueConverter
{
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolVal = value is bool b && b;
        bool inverted = !boolVal;

        if (targetType == typeof(System.Windows.Visibility))
            return inverted ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        return inverted;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Windows.Visibility v)
            return v != System.Windows.Visibility.Visible;

        if (value is bool b)
            return !b;

        return false;
    }

    #endregion
}

