using System.Globalization;

namespace DiskSpaceAnalyzer.Core.Formatting;

/// <summary>Formatea con base 1024 (KB, MB, GB…), igual que el Explorador de Windows.</summary>
public sealed class BinarySizeFormatter : ISizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    private readonly IFormatProvider _formatProvider;

    public BinarySizeFormatter(IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
    }

    public string Format(long sizeInBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);

        double value = sizeInBytes;
        var unit = 0;

        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? string.Create(_formatProvider, $"{sizeInBytes} B")
            : string.Create(_formatProvider, $"{value:0.##} {Units[unit]}");
    }
}
