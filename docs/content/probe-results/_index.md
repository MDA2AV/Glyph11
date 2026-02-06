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

## Glossary

<div id="glossary"></div>

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
    var scored = s.scored || s.total;
    var pct = Math.round((s.passed / scored) * 100);
    var bg = pct === 100 ? '#1a7f37' : pct >= 80 ? '#9a6700' : '#cf222e';
    html += '<div style="background:' + bg + ';color:#fff;border-radius:6px;padding:6px 12px;text-align:center;line-height:1.3;">';
    html += '<div style="font-weight:700;font-size:12px;">' + sv.name + '</div>';
    html += '<div style="font-size:16px;font-weight:800;">' + s.passed + '/' + scored + '</div>';
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
  var WARN_BG = '#9a6700';
  var FAIL_BG = '#cf222e';
  var SKIP_BG = '#656d76';
  var EXPECT_BG = '#444c56';
  var pillCss = 'text-align:center;padding:2px 4px;font-size:11px;font-weight:600;color:#fff;border-radius:3px;min-width:28px;display:inline-block;line-height:18px;';

  function pill(bg, label) {
    return '<span style="' + pillCss + 'background:' + bg + ';">' + label + '</span>';
  }

  function verdictBg(v) {
    return v === 'Pass' ? PASS_BG : v === 'Warn' ? WARN_BG : FAIL_BG;
  }

  // Track all tests for glossary
  var allTests = [];

  function renderTable(targetId, categoryKey) {
    var el = document.getElementById(targetId);
    var catTests = testIds.filter(function (tid) {
      return lookup[names[0]][tid] && lookup[names[0]][tid].category === categoryKey;
    });
    if (catTests.length === 0) {
      el.innerHTML = '<p><em>No tests in this category.</em></p>';
      return;
    }

    // Split into scored and unscored
    var scoredTests = catTests.filter(function (tid) { var r = lookup[names[0]][tid]; return r.scored !== false; });
    var unscoredTests = catTests.filter(function (tid) { var r = lookup[names[0]][tid]; return r.scored === false; });

    // Collect for glossary (scored first, then unscored)
    scoredTests.forEach(function (tid) { allTests.push(tid); });
    unscoredTests.forEach(function (tid) { allTests.push(tid); });

    // Reorder: scored tests first, then unscored
    var orderedTests = scoredTests.concat(unscoredTests);
    var shortLabels = orderedTests.map(function (tid) {
      return tid.replace(/^(RFC\d+-[\d.]+-|COMP-|SMUG-|MAL-)/, '');
    });

    var t = '<div style="overflow-x:auto;"><table style="border-collapse:collapse;font-size:12px;white-space:nowrap;">';

    // ── Column header row ──
    t += '<thead><tr>';
    t += '<th style="padding:4px 8px;text-align:left;vertical-align:bottom;min-width:100px;"></th>';
    orderedTests.forEach(function (tid, i) {
      var first = lookup[names[0]][tid];
      var isUnscored = first.scored === false;
      var opacity = isUnscored ? 'opacity:0.55;' : '';
      t += '<th style="padding:3px 4px;text-align:center;vertical-align:bottom;' + opacity + '">';
      t += '<a href="#test-' + tid + '" style="font-size:10px;font-weight:500;color:inherit;text-decoration:none;" title="' + first.description + '">' + shortLabels[i];
      if (isUnscored) t += '*';
      t += '</a></th>';
    });
    t += '</tr></thead><tbody>';

    // ── Expected row ──
    t += '<tr style="background:#f6f8fa;">';
    t += '<td style="padding:4px 8px;font-weight:700;font-size:11px;color:#656d76;">Expected</td>';
    orderedTests.forEach(function (tid) {
      var first = lookup[names[0]][tid];
      var isUnscored = first.scored === false;
      var opacity = isUnscored ? 'opacity:0.55;' : '';
      t += '<td style="text-align:center;padding:2px 3px;' + opacity + '">' + pill(EXPECT_BG, first.expected.replace(/ or close/g, '/\u2715').replace(/\//g, '/\u200B')) + '</td>';
    });
    t += '</tr>';

    // ── Server rows ──
    names.forEach(function (n) {
      t += '<tr>';
      t += '<td style="padding:4px 8px;font-weight:600;font-size:12px;">' + n + '</td>';
      orderedTests.forEach(function (tid) {
        var r = lookup[n] && lookup[n][tid];
        var isUnscored = lookup[names[0]][tid].scored === false;
        var opacity = isUnscored ? 'opacity:0.55;' : '';
        if (!r) {
          t += '<td style="text-align:center;padding:2px 3px;' + opacity + '">' + pill(SKIP_BG, '\u2014') + '</td>';
          return;
        }
        t += '<td style="text-align:center;padding:2px 3px;' + opacity + '">' + pill(verdictBg(r.verdict), r.got) + '</td>';
      });
      t += '</tr>';
    });

    t += '</tbody></table></div>';
    if (unscoredTests.length > 0) {
      t += '<p style="font-size:0.8em;color:#656d76;margin-top:4px;">* Not scored — RFC-compliant behavior, shown for reference.</p>';
    }
    el.innerHTML = t;
  }

  renderTable('table-compliance', 'Compliance');
  renderTable('table-smuggling', 'Smuggling');
  renderTable('table-malformed', 'MalformedInput');

  // ── Glossary ────────────────────────────────────────────────────────
  var glossaryEl = document.getElementById('glossary');
  if (glossaryEl && allTests.length > 0) {
    // Split scored and unscored
    var scoredIds = allTests.filter(function (tid) { return lookup[names[0]][tid].scored !== false; });
    var unscoredIds = allTests.filter(function (tid) { return lookup[names[0]][tid].scored === false; });

    var g = '<div style="max-height:500px;overflow-y:auto;border:1px solid #d0d7de;border-radius:6px;">';
    g += '<table style="border-collapse:collapse;font-size:13px;width:100%;">';
    g += '<thead style="position:sticky;top:0;background:#fff;z-index:1;"><tr>';
    g += '<th style="text-align:left;padding:6px 8px;border-bottom:2px solid #d0d7de;width:260px;">Test ID</th>';
    g += '<th style="text-align:center;padding:6px 8px;border-bottom:2px solid #d0d7de;width:100px;">Expected</th>';
    g += '<th style="text-align:left;padding:6px 8px;border-bottom:2px solid #d0d7de;">Description</th>';
    g += '</tr></thead><tbody>';

    function glossaryRow(tid) {
      var r = lookup[names[0]][tid];
      if (!r) return '';
      var rfc = r.rfc ? ' <span style="color:#656d76;font-size:11px;">(' + r.rfc + ')</span>' : '';
      var row = '<tr id="test-' + tid + '" style="border-bottom:1px solid #f0f0f0;">';
      row += '<td style="padding:5px 8px;"><code style="font-size:12px;">' + tid + '</code>' + rfc + '</td>';
      row += '<td style="text-align:center;padding:5px 8px;">' + pill(EXPECT_BG, r.expected) + '</td>';
      row += '<td style="padding:5px 8px;">' + r.reason + '</td>';
      row += '</tr>';
      return row;
    }

    scoredIds.forEach(function (tid) { g += glossaryRow(tid); });

    if (unscoredIds.length > 0) {
      g += '<tr><td colspan="3" style="padding:8px;font-weight:700;font-size:12px;color:#656d76;background:#f6f8fa;border-bottom:1px solid #d0d7de;">Not scored (RFC-compliant behavior)</td></tr>';
      unscoredIds.forEach(function (tid) { g += glossaryRow(tid); });
    }

    g += '</tbody></table></div>';
    glossaryEl.innerHTML = g;
  }
})();
</script>
