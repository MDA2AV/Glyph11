using Glyph11.Probe.Client;
using Glyph11.Probe.Response;

namespace Glyph11.Probe.TestCases;

public sealed class TestResult
{
    public required TestCase TestCase { get; init; }
    public required TestVerdict Verdict { get; init; }
    public HttpResponse? Response { get; init; }
    public HttpResponse? FollowUpResponse { get; init; }
    public ConnectionState ConnectionState { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}
