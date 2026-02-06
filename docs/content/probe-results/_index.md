---
title: Probe Results
layout: wide
toc: false
---

HTTP/1.1 compliance comparison across frameworks. Each test sends a specific malformed or ambiguous request and checks the server's response against the **exact** expected status code (e.g. a parser error must return `400`, not `404`). Updated on each manual probe run on `main`.

## Summary

<div id="probe-summary"><p><em>Loading probe data...</em></p></div>

{{< callout type="info" >}}
These results are from CI runs (`ubuntu-latest`). Servers tested: **Glyph11** (raw TCP + HardenedParser), **Kestrel** (ASP.NET Core), **Flask** (Python), **Express** (Node.js), **Spring Boot** (Java), **Quarkus** (Java), **Nancy** (.NET), **Jetty** (Java), **Nginx** (native), **Apache** (native), **Caddy** (native).
{{< /callout >}}

## Compliance

RFC 9110/9112 protocol requirements. These tests verify the parser rejects malformed framing per the HTTP specification.

<div id="table-compliance"></div>

## Smuggling

HTTP request smuggling vectors. These tests verify the parser rejects ambiguous requests that could be interpreted differently by intermediaries.

<div id="table-smuggling"></div>

## Malformed Input

Robustness tests for garbage, oversized, and invalid payloads. These tests verify the server handles pathological input without crashing.

<div id="table-malformed"></div>

<script src="/Glyph11/probe/data.js"></script>
<script>
(function () {
  var summary = document.getElementById('probe-summary');

  if (!window.PROBE_DATA) {
    summary.innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }

  var data = window.PROBE_DATA;
  var servers = data.servers;
  if (!servers || servers.length === 0) {
    summary.innerHTML = '<p><em>No server results found.</em></p>';
    return;
  }

  // ── Summary badges ────────────────────────────────────────────────
  var html = '<div style="display:flex;gap:16px;flex-wrap:wrap;align-items:center;">';
  servers.forEach(function (sv) {
    var s = sv.summary;
    var pct = Math.round((s.passed / s.total) * 100);
    var color = pct === 100 ? '#178600' : pct >= 80 ? '#b08800' : '#d73a49';
    html += '<div style="border:1px solid #e5e7eb;border-radius:8px;padding:12px 20px;text-align:center;">';
    html += '<div style="font-weight:bold;font-size:1.1em;">' + sv.name + '</div>';
    html += '<div style="font-size:1.8em;font-weight:bold;color:' + color + ';">' + s.passed + '/' + s.total + '</div>';
    html += '<div style="font-size:0.85em;color:#666;">' + pct + '% compliant</div>';
    html += '</div>';
  });
  html += '</div>';
  if (data.commit) {
    html += '<p style="margin-top:8px;font-size:0.85em;color:#666;">Commit: <code>' + data.commit.id.substring(0, 7) + '</code> &mdash; ' + (data.commit.message || '') + '</p>';
  }
  summary.innerHTML = html;

  // ── Build lookups ─────────────────────────────────────────────────
  var names = servers.map(function (sv) { return sv.name; });
  var lookup = {};
  servers.forEach(function (sv) {
    var m = {};
    sv.results.forEach(function (r) { m[r.id] = r; });
    lookup[sv.name] = m;
  });

  // Test IDs from first server (canonical order)
  var testIds = servers[0].results.map(function (r) { return r.id; });

  // ── Render comparison table ───────────────────────────────────────
  function renderTable(targetId, categoryKey) {
    var el = document.getElementById(targetId);
    var catTests = testIds.filter(function (tid) {
      return lookup[names[0]][tid] && lookup[names[0]][tid].category === categoryKey;
    });
    if (catTests.length === 0) {
      el.innerHTML = '<p><em>No tests in this category.</em></p>';
      return;
    }

    var t = '<table style="width:100%;font-size:0.9em;border-collapse:collapse;">';

    // Header
    t += '<thead><tr>';
    t += '<th style="text-align:left;padding:8px 6px;">Test</th>';
    t += '<th style="text-align:center;padding:8px 6px;">Expected</th>';
    names.forEach(function (n) {
      t += '<th style="text-align:center;padding:8px 6px;min-width:90px;">' + n + '</th>';
    });
    t += '<th style="text-align:left;padding:8px 6px;">Reason</th>';
    t += '</tr></thead><tbody>';

    catTests.forEach(function (tid) {
      var first = lookup[names[0]][tid];
      var allPass = names.every(function (n) {
        var r = lookup[n][tid];
        return r && r.verdict === 'Pass';
      });

      t += '<tr>';

      // Test ID + description
      t += '<td style="padding:6px;vertical-align:top;">';
      t += '<code style="font-size:0.9em;">' + tid + '</code>';
      t += '<br><span style="font-size:0.8em;color:#666;">' + first.description + '</span>';
      t += '</td>';

      // Expected
      t += '<td style="text-align:center;padding:6px;vertical-align:top;"><code>' + first.expected + '</code></td>';

      // Per-server verdict
      names.forEach(function (n) {
        var r = lookup[n][tid];
        if (!r) {
          t += '<td style="text-align:center;padding:6px;">\u2014</td>';
          return;
        }
        var icon = r.verdict === 'Pass' ? '\u2705' : '\u274c';
        var bg = r.verdict === 'Pass' ? '' : 'background:#fff5f5;';
        t += '<td style="text-align:center;padding:6px;' + bg + '">';
        t += icon + ' <code>' + r.got + '</code>';
        t += '</td>';
      });

      // Reason (from strict spec, not per-server)
      t += '<td style="padding:6px;font-size:0.85em;vertical-align:top;">' + first.reason + '</td>';

      t += '</tr>';
    });

    t += '</tbody></table>';
    el.innerHTML = t;
  }

  renderTable('table-compliance', 'Compliance');
  renderTable('table-smuggling', 'Smuggling');
  renderTable('table-malformed', 'MalformedInput');
})();
</script>
