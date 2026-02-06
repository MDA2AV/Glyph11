using System.Text;
using Glyph11.Probe.Client;
using Glyph11.Probe.Response;

namespace Glyph11.Probe.TestCases.Suites;

public static class SmugglingSuite
{
    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "SMUG-CL-TE-BOTH",
            Description = "Both Content-Length and Transfer-Encoding present must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 6\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-DUPLICATE-CL",
            Description = "Duplicate Content-Length with different values must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nContent-Length: 10\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-LEADING-ZEROS",
            Description = "Content-Length with leading zeros should be rejected",
            Category = TestCategory.Smuggling,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 005\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-XCHUNKED",
            Description = "Transfer-Encoding: xchunked must not be treated as chunked",
            Category = TestCategory.Smuggling,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: xchunked\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                // Server should either reject (400) or use CL, not treat as chunked
                ExpectedStatus = StatusCodeRange.Range4xxOr5xx,
                AllowConnectionClose = true,
                CustomValidator = (response, state) =>
                {
                    // 400 = strict rejection (best), 501 = unknown TE (ok), 2xx with CL-based body = acceptable
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode >= 400)
                        return TestVerdict.Pass;
                    // 2xx means server ignored unknown TE and used CL — acceptable but warn
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-TRAILING-SPACE",
            Description = "Transfer-Encoding: 'chunked ' (trailing space) must not be treated as chunked",
            Category = TestCategory.Smuggling,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked \r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode >= 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-SP-BEFORE-COLON",
            Description = "Transfer-Encoding with space before colon must be rejected",
            Category = TestCategory.Smuggling,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding : chunked\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-NEGATIVE",
            Description = "Negative Content-Length must be rejected",
            Category = TestCategory.Smuggling,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: -1\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CLTE-PIPELINE",
            Description = "CL.TE smuggling probe — follow-up should not receive smuggled response",
            Category = TestCategory.Smuggling,
            RequiresConnectionReuse = true,
            PayloadFactory = ctx =>
            {
                // Ambiguous: CL says body is 4 bytes ("0\r\n\r"), but TE chunked says 0 chunk = end
                // A CL-only parser reads 4 bytes and waits; a TE parser sees end-of-chunks
                var body = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 4\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n";
                return Encoding.ASCII.GetBytes(body);
            },
            FollowUpPayloadFactory = ctx =>
                Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    // Best: server rejects the ambiguous request with 400
                    if (response is not null && response.StatusCode >= 400)
                        return TestVerdict.Pass;
                    // Connection closed = safe
                    if (state == ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    // If we got a normal 2xx, might be vulnerable — warn
                    return TestVerdict.Warn;
                }
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
