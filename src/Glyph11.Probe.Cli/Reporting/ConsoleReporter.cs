using Glyph11.Probe.Runner;
using Glyph11.Probe.TestCases;

namespace Glyph11.Probe.Cli.Reporting;

public static class ConsoleReporter
{
    public static void Print(TestRunReport report)
    {
        var scored = report.Results.Where(r => r.Verdict is TestVerdict.Pass or TestVerdict.Fail or TestVerdict.Error).ToList();
        var warnings = report.Results.Where(r => r.Verdict == TestVerdict.Warn).ToList();

        Console.WriteLine();
        Console.WriteLine("  {0,-35} {1,-10} {2,-6} {3}", "Test ID", "Verdict", "Status", "Details");
        Console.WriteLine("  " + new string('─', 80));

        foreach (var result in scored)
            PrintRow(result);

        if (warnings.Count > 0)
        {
            Console.WriteLine();
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ┄┄ Not scored (RFC-compliant behavior) ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄");
            Console.ForegroundColor = prev;

            foreach (var result in warnings)
                PrintRow(result);
        }

        Console.WriteLine("  " + new string('─', 80));
        Console.WriteLine();

        // Score = pass + fail only (warnings are informational, not scored)
        var scoredCount = report.PassCount + report.FailCount;
        var prev2 = Console.ForegroundColor;
        Console.Write("  Score: ");
        Console.ForegroundColor = report.FailCount == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($"{report.PassCount}/{scoredCount}");
        Console.ForegroundColor = prev2;

        if (report.FailCount > 0)
        {
            Console.Write(" (");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{report.FailCount} failed");
            Console.ForegroundColor = prev2;
            Console.Write(")");
        }

        if (report.WarnCount > 0)
        {
            Console.Write("  ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{report.WarnCount} warnings");
            Console.ForegroundColor = prev2;
        }

        if (report.ErrorCount > 0)
        {
            Console.Write("  ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{report.ErrorCount} errors");
            Console.ForegroundColor = prev2;
        }

        if (report.SkipCount > 0)
            Console.Write($"  {report.SkipCount} skipped");

        Console.WriteLine($"  ({report.TotalDuration.TotalSeconds:F1}s)");
        Console.WriteLine();
    }

    private static void PrintRow(TestResult result)
    {
        var (color, symbol) = result.Verdict switch
        {
            TestVerdict.Pass => (ConsoleColor.Green, "PASS"),
            TestVerdict.Fail => (ConsoleColor.Red, "FAIL"),
            TestVerdict.Warn => (ConsoleColor.Yellow, "WARN"),
            TestVerdict.Error => (ConsoleColor.Magenta, "ERR "),
            _ => (ConsoleColor.Gray, "SKIP")
        };

        var statusStr = result.Response is not null
            ? result.Response.StatusCode.ToString()
            : result.ConnectionState.ToString();

        var detail = result.ErrorMessage
                     ?? result.Response?.ReasonPhrase
                     ?? string.Empty;

        if (detail.Length > 30)
            detail = detail[..30] + "...";

        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write("  {0,-35} {1,-10}", result.TestCase.Id, symbol);
        Console.ForegroundColor = prev;
        Console.WriteLine(" {0,-6} {1}", statusStr, detail);
    }
}
