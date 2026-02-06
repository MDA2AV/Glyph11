using System.Diagnostics;
using Glyph11.Probe.Client;
using Glyph11.Probe.Response;
using Glyph11.Probe.TestCases;

namespace Glyph11.Probe.Runner;

public sealed class TestRunner
{
    private readonly TestRunOptions _options;

    public TestRunner(TestRunOptions options)
    {
        _options = options;
    }

    public async Task<TestRunReport> RunAsync(IEnumerable<TestCase> testCases)
    {
        var results = new List<TestResult>();
        var totalSw = Stopwatch.StartNew();

        var context = new TestContext
        {
            Host = _options.Host,
            Port = _options.Port
        };

        foreach (var testCase in testCases)
        {
            if (_options.CategoryFilter.HasValue && testCase.Category != _options.CategoryFilter.Value)
            {
                results.Add(new TestResult
                {
                    TestCase = testCase,
                    Verdict = TestVerdict.Skip,
                    ConnectionState = ConnectionState.Open,
                    Duration = TimeSpan.Zero
                });
                continue;
            }

            results.Add(await RunSingleAsync(testCase, context));
        }

        totalSw.Stop();

        return new TestRunReport
        {
            Results = results,
            TotalDuration = totalSw.Elapsed
        };
    }

    private async Task<TestResult> RunSingleAsync(TestCase testCase, TestContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await using var client = new RawTcpClient(_options.ConnectTimeout, _options.ReadTimeout);
            var connectState = await client.ConnectAsync(_options.Host, _options.Port);

            if (connectState != ConnectionState.Open)
            {
                return new TestResult
                {
                    TestCase = testCase,
                    Verdict = TestVerdict.Error,
                    ConnectionState = connectState,
                    ErrorMessage = $"Failed to connect: {connectState}",
                    Duration = sw.Elapsed
                };
            }

            // Send the primary payload
            var payload = testCase.PayloadFactory(context);
            await client.SendAsync(payload);

            // Read primary response
            var (data, length, readState) = await client.ReadResponseAsync();
            var response = ResponseParser.TryParse(data.AsSpan(), length);

            HttpResponse? followUpResponse = null;
            var connectionState = readState;

            // If follow-up is needed and connection is still open
            if (testCase.FollowUpPayloadFactory is not null && connectionState == ConnectionState.Open)
            {
                var followUpPayload = testCase.FollowUpPayloadFactory(context);
                await client.SendAsync(followUpPayload);

                var (fuData, fuLength, fuState) = await client.ReadResponseAsync();
                followUpResponse = ResponseParser.TryParse(fuData.AsSpan(), fuLength);
                connectionState = fuState;
            }
            else if (connectionState == ConnectionState.Open && !testCase.RequiresConnectionReuse)
            {
                // Brief pause then check if server closed the connection
                await Task.Delay(50);
                connectionState = client.CheckConnectionState();
            }

            var verdict = testCase.Expected.Evaluate(response, connectionState);

            return new TestResult
            {
                TestCase = testCase,
                Verdict = verdict,
                Response = response,
                FollowUpResponse = followUpResponse,
                ConnectionState = connectionState,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestCase = testCase,
                Verdict = TestVerdict.Error,
                ConnectionState = ConnectionState.Error,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }
}
