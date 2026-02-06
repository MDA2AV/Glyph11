namespace Glyph11.Probe.TestCases;

public sealed class StatusCodeRange
{
    private readonly int? _exact;
    private readonly int? _rangeStart;
    private readonly int? _rangeEnd;

    private StatusCodeRange(int? exact, int? rangeStart, int? rangeEnd)
    {
        _exact = exact;
        _rangeStart = rangeStart;
        _rangeEnd = rangeEnd;
    }

    public static StatusCodeRange Exact(int code) => new(code, null, null);
    public static StatusCodeRange Range(int start, int end) => new(null, start, end);

    public static StatusCodeRange Range2xx => Range(200, 299);
    public static StatusCodeRange Range4xx => Range(400, 499);
    public static StatusCodeRange Range4xxOr5xx => Range(400, 599);

    public bool Contains(int statusCode)
    {
        if (_exact.HasValue)
            return statusCode == _exact.Value;

        return statusCode >= _rangeStart!.Value && statusCode <= _rangeEnd!.Value;
    }

    public override string ToString()
    {
        if (_exact.HasValue)
            return _exact.Value.ToString();

        return $"{_rangeStart}-{_rangeEnd}";
    }
}
