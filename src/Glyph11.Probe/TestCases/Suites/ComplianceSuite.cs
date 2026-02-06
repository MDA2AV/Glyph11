using System.Text;
using Glyph11.Probe.Client;

namespace Glyph11.Probe.TestCases.Suites;

public static class ComplianceSuite
{
    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "COMP-BASELINE",
            Description = "Valid GET request — confirms server is reachable",
            Category = TestCategory.Compliance,
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.2-BARE-LF-REQUEST-LINE",
            Description = "Bare LF in request line must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.2-BARE-LF-HEADER",
            Description = "Bare LF in header must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\nX-Test: value\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-5.1-OBS-FOLD",
            Description = "Obs-fold (line folding) in headers should be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §5.1",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: value\r\n continued\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9110-5.6.2-SP-BEFORE-COLON",
            Description = "Whitespace between header name and colon must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §5.6.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test : value\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-3-MULTI-SP-REQUEST-LINE",
            Description = "Multiple spaces between request-line components should be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3",
            PayloadFactory = ctx => MakeRequest($"GET  / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-7.1-MISSING-HOST",
            Description = "Request without Host header must be rejected with 400",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = _ => MakeRequest("GET / HTTP/1.1\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.3-INVALID-VERSION",
            Description = "Invalid HTTP version must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/9.9\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xxOr5xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-5-EMPTY-HEADER-NAME",
            Description = "Empty header name (leading colon) must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §5",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n: empty-name\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-3-CR-ONLY-LINE-ENDING",
            Description = "CR without LF as line ending must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\rHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
