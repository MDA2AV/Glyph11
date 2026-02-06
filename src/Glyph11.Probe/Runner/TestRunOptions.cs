using Glyph11.Probe.TestCases;

namespace Glyph11.Probe.Runner;

public sealed class TestRunOptions
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public TestCategory? CategoryFilter { get; init; }
}
