using System.CommandLine;
using Glyph11.Probe.Cli.Reporting;
using Glyph11.Probe.Runner;
using Glyph11.Probe.TestCases;
using Glyph11.Probe.TestCases.Suites;

var hostOption = new Option<string>("--host") { Description = "Target host" };
hostOption.DefaultValueFactory = _ => "localhost";

var portOption = new Option<int>("--port") { Description = "Target port" };
portOption.DefaultValueFactory = _ => 8080;

var categoryOption = new Option<TestCategory?>("--category") { Description = "Run only tests in this category" };

var timeoutOption = new Option<int>("--timeout") { Description = "Read/connect timeout in seconds" };
timeoutOption.DefaultValueFactory = _ => 5;

var outputOption = new Option<string?>("--output") { Description = "Write JSON report to this file path" };

var rootCommand = new RootCommand("Glyph11.Probe — HTTP/1.1 server compliance & hardening tester")
{
    hostOption,
    portOption,
    categoryOption,
    timeoutOption,
    outputOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var host = parseResult.GetValue(hostOption)!;
    var port = parseResult.GetValue(portOption);
    var category = parseResult.GetValue(categoryOption);
    var timeout = parseResult.GetValue(timeoutOption);
    var outputPath = parseResult.GetValue(outputOption);

    Console.WriteLine($"  Glyph11.Probe targeting {host}:{port}");
    Console.WriteLine();

    var options = new TestRunOptions
    {
        Host = host,
        Port = port,
        ConnectTimeout = TimeSpan.FromSeconds(timeout),
        ReadTimeout = TimeSpan.FromSeconds(timeout),
        CategoryFilter = category
    };

    var testCases = ComplianceSuite.GetTestCases()
        .Concat(SmugglingSuite.GetTestCases())
        .Concat(MalformedInputSuite.GetTestCases())
        .ToList();

    var runner = new TestRunner(options);
    var report = await runner.RunAsync(testCases);

    ConsoleReporter.Print(report);

    if (outputPath is not null)
    {
        var json = JsonReporter.Generate(report);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
        Console.WriteLine($"  JSON report written to {outputPath}");
    }
});

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
