using System.Text;
using Glyph11.Probe.Client;

namespace Glyph11.Probe.TestCases.Suites;

public static class MalformedInputSuite
{
    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "MAL-BINARY-GARBAGE",
            Description = "Random binary garbage should be rejected or connection closed",
            Category = TestCategory.MalformedInput,
            PayloadFactory = _ =>
            {
                var rng = new Random(42);
                var garbage = new byte[256];
                rng.NextBytes(garbage);
                return garbage;
            },
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-LONG-URL",
            Description = "100KB URL should be rejected with 414 URI Too Long",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var longPath = "/" + new string('A', 100_000);
                return MakeRequest($"GET {longPath} HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    // 414 is ideal, but any 4xx is acceptable
                    return response.StatusCode >= 400 && response.StatusCode < 600
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-LONG-HEADER-VALUE",
            Description = "100KB header value should be rejected with 431",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var longValue = new string('B', 100_000);
                return MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Big: {longValue}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode >= 400 && response.StatusCode < 600
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-MANY-HEADERS",
            Description = "10,000 headers should be rejected with 431",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var sb = new StringBuilder();
                sb.Append($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n");
                for (var i = 0; i < 10_000; i++)
                    sb.Append($"X-H-{i}: value\r\n");
                sb.Append("\r\n");
                return Encoding.ASCII.GetBytes(sb.ToString());
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode >= 400 && response.StatusCode < 600
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-NUL-IN-URL",
            Description = "NUL byte in URL should be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx => MakeRequest($"GET /\0test HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CONTROL-CHARS-HEADER",
            Description = "Control characters in header value should be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                // Inject BEL (0x07), BS (0x08), VT (0x0B) into header value
                var request = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: abc\x07\x08\x0Bdef\r\n\r\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range4xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-INCOMPLETE-REQUEST",
            Description = "Incomplete request line (just 'GET ') should timeout or close, not crash",
            Category = TestCategory.MalformedInput,
            PayloadFactory = _ => MakeRequest("GET "),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    // Any of these is acceptable: timeout, close, or 400
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode >= 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-EMPTY-REQUEST",
            Description = "Empty request (just CRLF) should be handled gracefully",
            Category = TestCategory.MalformedInput,
            PayloadFactory = _ => MakeRequest("\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode >= 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
