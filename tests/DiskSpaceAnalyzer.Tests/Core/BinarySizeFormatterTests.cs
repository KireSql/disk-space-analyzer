using System.Globalization;
using DiskSpaceAnalyzer.Core.Formatting;

namespace DiskSpaceAnalyzer.Tests.Core;

public class BinarySizeFormatterTests
{
    private readonly BinarySizeFormatter _formatter = new(CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1_048_576, "1 MB")]
    [InlineData(5_368_709_120, "5 GB")]
    [InlineData(1_099_511_627_776, "1 TB")]
    public void Format_UsesBinaryUnits(long bytes, string expected)
        => Assert.Equal(expected, _formatter.Format(bytes));

    [Fact]
    public void Format_RespectsTheProvidedCulture()
    {
        var formatter = new BinarySizeFormatter(new CultureInfo("es-ES"));

        Assert.Equal("1,5 KB", formatter.Format(1536));
    }

    [Fact]
    public void Format_RejectsNegativeSizes()
        => Assert.Throws<ArgumentOutOfRangeException>(() => _formatter.Format(-1));
}
