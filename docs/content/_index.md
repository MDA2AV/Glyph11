---
title: Glyph11
layout: hextra-home
---

<div style="margin-top: 1rem; margin-bottom: 1rem;">
{{< hextra/hero-badge link="https://www.nuget.org/packages/Glyph11/" >}}
  <span>NuGet</span>
  {{< icon name="arrow-circle-right" attributes="height=14" >}}
{{< /hextra/hero-badge >}}
</div>

<div style="margin-top: 1.25rem; margin-bottom: 1.25rem;">
{{< hextra/hero-headline >}}
  Zero-Allocation HTTP/1.1 Parsing
{{< /hextra/hero-headline >}}
</div>

<div style="margin-bottom: 1.5rem;">
{{< hextra/hero-subtitle >}}
  A dependency-free, low-allocation HTTP/1.1 header parser for C#&nbsp;<br class="sm:hx-block hx-hidden" />built on Span&lt;T&gt; and ReadOnlySequence&lt;byte&gt;.
{{< /hextra/hero-subtitle >}}
</div>

<div style="margin-bottom: 2.5rem;">
{{< hextra/hero-button text="Get Started" link="docs/getting-started" >}}
</div>

<div style="max-width: 56rem;">
{{< hextra/feature-grid >}}
  {{< hextra/feature-card
    title="Zero Allocation"
    subtitle="ROM path produces zero heap allocations. All parsed fields are ReadOnlyMemory<byte> slices into the original buffer."
    style="background: radial-gradient(ellipse at 50% 80%,rgba(72,120,198,0.15),hsla(0,0%,100%,0)); min-height: 18rem;"
  >}}
  {{< hextra/feature-card
    title="Security Hardened"
    subtitle="RFC 9110/9112 validation with configurable resource limits. Detects request smuggling, path traversal, and protocol violations."
    style="background: radial-gradient(ellipse at 50% 80%,rgba(198,72,72,0.15),hsla(0,0%,100%,0)); min-height: 18rem;"
  >}}
  {{< hextra/feature-card
    title="Network Agnostic"
    subtitle="Works with any network stack: Socket, NetworkStream, PipeReader, or anything that produces ReadOnlySequence<byte>."
    style="background: radial-gradient(ellipse at 50% 80%,rgba(72,198,120,0.15),hsla(0,0%,100%,0)); min-height: 18rem;"
  >}}
{{< /hextra/feature-grid >}}
</div>
