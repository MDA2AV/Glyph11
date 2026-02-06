---
title: Probe Results
layout: wide
toc: false
---

HTTP/1.1 compliance comparison across frameworks. Each test sends a specific malformed or ambiguous request and checks the server's response against the **exact** expected status code (e.g. a parser error must return `400`, not `404`). Updated on each manual probe run on `main`.

## Summary

<div id="probe-summary"><p><em>Loading probe data...</em></p></div>

{{< callout type="info" >}}
These results are from CI runs (`ubuntu-latest`). Servers tested: **Glyph11** (raw TCP + HardenedParser), **Kestrel** (ASP.NET Core), **Flask** (Python), **Express** (Node.js), **Spring Boot** (Java), **Quarkus** (Java), **Nancy** (.NET), **Jetty** (Java), **Nginx** (native), **Apache** (native), **Caddy** (native), **Pingora** (Rust).
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
  var html = '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">';
  servers.forEach(function (sv) {
    var s = sv.summary;
    var pct = Math.round((s.passed / s.total) * 100);
    var bg = pct === 100 ? '#1a7f37' : pct >= 80 ? '#9a6700' : '#cf222e';
    html += '<div style="background:' + bg + ';color:#fff;border-radius:6px;padding:6px 12px;text-align:center;line-height:1.3;">';
    html += '<div style="font-weight:700;font-size:12px;">' + sv.name + '</div>';
    html += '<div style="font-size:16px;font-weight:800;">' + s.passed + '/' + s.total + '</div>';
    html += '</div>';
  });
  html += '</div>';
  if (data.commit) {
    html += '<p style="margin-top:8px;font-size:0.85em;color:#656d76;">Commit: <code>' + data.commit.id.substring(0, 7) + '</code> &mdash; ' + (data.commit.message || '') + '</p>';
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

  // ── Render transposed table (servers = rows, tests = columns) ────
  var PASS_BG = '#1a7f37';
  var FAIL_BG = '#cf222e';
  var SKIP_BG = '#656d76';
  var EXPECT_BG = '#444c56';
  var cellCss = 'text-align:center;padding:2px 3px;font-size:11px;font-weight:600;color:#fff;';
  var pillCss = cellCss + 'border-radius:3px;min-width:28px;display:inline-block;line-height:18px;';

  function pill(bg, label) {
    return '<span style="' + pillCss + 'background:' + bg + ';">' + label + '</span>';
  }

  function renderTable(targetId, categoryKey) {
    var el = document.getElementById(targetId);
    var catTests = testIds.filter(function (tid) {
      return lookup[names[0]][tid] && lookup[names[0]][tid].category === categoryKey;
    });
    if (catTests.length === 0) {
      el.innerHTML = '<p><em>No tests in this category.</em></p>';
      return;
    }

    // Short labels: strip common prefixes for column headers
    var shortLabels = catTests.map(function (tid) {
      return tid.replace(/^(RFC\d+-[\d.]+-|COMP-|SMUG-|MAL-)/, '');
    });

    var t = '<div style="overflow-x:auto;"><table style="border-collapse:collapse;font-size:12px;white-space:nowrap;">';

    // ── Column header row (test IDs, rotated) ──
    t += '<thead><tr>';
    t += '<th style="padding:4px 8px;text-align:left;vertical-align:bottom;min-width:100px;"></th>';
    catTests.forEach(function (tid, i) {
      var first = lookup[names[0]][tid];
      t += '<th style="padding:2px 3px;vertical-align:bottom;text-align:center;" title="' + first.description + '">';
      t += '<span style="writing-mode:vertical-rl;transform:rotate(180deg);font-size:10px;font-weight:500;letter-spacing:-0.3px;">' + shortLabels[i] + '</span>';
      t += '</th>';
    });
    t += '</tr></thead><tbody>';

    // ── Expected row ──
    t += '<tr style="background:#f6f8fa;">';
    t += '<td style="padding:4px 8px;font-weight:700;font-size:11px;color:#656d76;">Expected</td>';
    catTests.forEach(function (tid) {
      var first = lookup[names[0]][tid];
      t += '<td style="text-align:center;padding:2px 3px;">' + pill(EXPECT_BG, first.expected.replace(/ or close/g, '/\u2715').replace(/\//g, '/\u200B')) + '</td>';
    });
    t += '</tr>';

    // ── Server rows ──
    names.forEach(function (n) {
      t += '<tr>';
      t += '<td style="padding:4px 8px;font-weight:600;font-size:12px;">' + n + '</td>';
      catTests.forEach(function (tid) {
        var r = lookup[n] && lookup[n][tid];
        if (!r) {
          t += '<td style="text-align:center;padding:2px 3px;">' + pill(SKIP_BG, '\u2014') + '</td>';
          return;
        }
        var bg = r.verdict === 'Pass' ? PASS_BG : FAIL_BG;
        t += '<td style="text-align:center;padding:2px 3px;">' + pill(bg, r.got) + '</td>';
      });
      t += '</tr>';
    });

    t += '</tbody></table></div>';
    el.innerHTML = t;
  }

  renderTable('table-compliance', 'Compliance');
  renderTable('table-smuggling', 'Smuggling');
  renderTable('table-malformed', 'MalformedInput');
})();
</script>
