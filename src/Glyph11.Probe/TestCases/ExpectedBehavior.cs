using Glyph11.Probe.Client;
using Glyph11.Probe.Response;

namespace Glyph11.Probe.TestCases;

public sealed class ExpectedBehavior
{
    public StatusCodeRange? ExpectedStatus { get; init; }
    public ConnectionState? ExpectedConnectionState { get; init; }
    public bool AllowConnectionClose { get; init; }
    public Func<HttpResponse?, ConnectionState, TestVerdict>? CustomValidator { get; init; }

    public TestVerdict Evaluate(HttpResponse? response, ConnectionState connectionState)
    {
        if (CustomValidator is not null)
            return CustomValidator(response, connectionState);

        if (ExpectedConnectionState.HasValue && connectionState == ExpectedConnectionState.Value)
            return TestVerdict.Pass;

        if (AllowConnectionClose && connectionState == ConnectionState.ClosedByServer && response is null)
            return TestVerdict.Pass;

        if (response is null)
            return AllowConnectionClose ? TestVerdict.Pass : TestVerdict.Fail;

        if (ExpectedStatus is not null && ExpectedStatus.Contains(response.StatusCode))
            return TestVerdict.Pass;

        return TestVerdict.Fail;
    }
}
