namespace Glyph11.Probe.TestCases;

public sealed class TestCase
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public required TestCategory Category { get; init; }
    public string? RfcReference { get; init; }
    public required Func<TestContext, byte[]> PayloadFactory { get; init; }
    public Func<TestContext, byte[]>? FollowUpPayloadFactory { get; init; }
    public required ExpectedBehavior Expected { get; init; }
    public bool RequiresConnectionReuse { get; init; }
}
