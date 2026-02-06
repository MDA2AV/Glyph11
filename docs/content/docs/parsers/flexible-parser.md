---
title: FlexibleParser
weight: 2
---

{{< callout type="info" >}}
The `FlexibleParser` has been removed from Glyph11. The `HardenedParser` is now the sole parser, providing both performance and security validation.
{{< /callout >}}

If you previously used `FlexibleParser`, migrate to `HardenedParser`:

```csharp
// Before (FlexibleParser — removed)
// FlexibleParser.TryExtractFullHeader(ref buffer, request, out bytesRead);

// After (HardenedParser)
var limits = ParserLimits.Default;
HardenedParser.TryExtractFullHeader(ref buffer, request, in limits, out bytesRead);
```

The `HardenedParser` API is identical except for the additional `ParserLimits` parameter. Use `ParserLimits.Default` for standard limits.
