using System.Text;

namespace Tests;

static class AssertAscii
{
    public static void Equal(string expectedAscii, ReadOnlyMemory<byte> actual)
        => Assert.Equal(expectedAscii, Encoding.ASCII.GetString(actual.Span));

    public static void Equal(string expectedAscii, ReadOnlySpan<byte> actual)
        => Assert.Equal(expectedAscii, Encoding.ASCII.GetString(actual));
}
