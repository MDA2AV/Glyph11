using System.Text.Json;
using System.Text.Json.Serialization;
using Glyph11.Probe.Runner;
using Glyph11.Probe.TestCases;

namespace Glyph11.Probe.Cli.Reporting;

public static class JsonReporter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Generate(TestRunReport report)
    {
        var output = new
        {
            summary = new
            {
                total = report.Results.Count,
                scored = report.PassCount + report.FailCount,
                passed = report.PassCount,
                failed = report.FailCount,
                warnings = report.WarnCount,
                errors = report.ErrorCount,
                skipped = report.SkipCount,
                durationMs = report.TotalDuration.TotalMilliseconds
            },
            results = report.Results.Select(r => new
            {
                id = r.TestCase.Id,
                description = r.TestCase.Description,
                category = r.TestCase.Category.ToString(),
                rfcReference = r.TestCase.RfcReference,
                verdict = r.Verdict.ToString(),
                statusCode = r.Response?.StatusCode,
                connectionState = r.ConnectionState.ToString(),
                error = r.ErrorMessage,
                durationMs = r.Duration.TotalMilliseconds
            })
        };

        return JsonSerializer.Serialize(output, s_options);
    }
}
