window.BENCHMARK_DATA = {
  "lastUpdate": 1770395907877,
  "repoUrl": "https://github.com/MDA2AV/Glyph11",
  "entries": {
    "Benchmark": [
      {
        "commit": {
          "author": {
            "name": "Diogo Martins",
            "username": "MDA2AV",
            "email": "diogoalves@ua.pt"
          },
          "committer": {
            "name": "Diogo Martins",
            "username": "MDA2AV",
            "email": "diogoalves@ua.pt"
          },
          "id": "448882638684c2a2874e9f52cc324002029fb52b",
          "message": "Redesign benchmarks to make whole process faster",
          "timestamp": "2026-02-06T16:20:06Z",
          "url": "https://github.com/MDA2AV/Glyph11/commit/448882638684c2a2874e9f52cc324002029fb52b"
        },
        "date": 1770395907039,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_Small",
            "value": 122.52550021807353,
            "unit": "ns",
            "range": "± 0.18765101165566078"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_4K",
            "value": 353.625850836436,
            "unit": "ns",
            "range": "± 0.7025070580457135"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_32K",
            "value": 2343.3859074910483,
            "unit": "ns",
            "range": "± 2.9537453672451064"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_ROM",
            "value": 137.2801253000895,
            "unit": "ns",
            "range": "± 0.2511778593713585"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_MultiSegment",
            "value": 358.58129898707074,
            "unit": "ns",
            "range": "± 2.412127073494532"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_ROM",
            "value": 881.6153984069824,
            "unit": "ns",
            "range": "± 4.12630871933195"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_MultiSegment",
            "value": 1989.939796447754,
            "unit": "ns",
            "range": "± 60.385570161827836"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_ROM",
            "value": 4875.074635823567,
            "unit": "ns",
            "range": "± 18.105629644138013"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_MultiSegment",
            "value": 14274.321090698242,
            "unit": "ns",
            "range": "± 128.5477051444546"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_ROM",
            "value": 191.64901940027872,
            "unit": "ns",
            "range": "± 0.1445774879919084"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_MultiSegment",
            "value": 417.1821395556132,
            "unit": "ns",
            "range": "± 0.9949506376133689"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_ROM",
            "value": 1118.9785499572754,
            "unit": "ns",
            "range": "± 8.422417044699511"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_MultiSegment",
            "value": 2393.441764831543,
            "unit": "ns",
            "range": "± 55.56037646175864"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_ROM",
            "value": 7930.661326090495,
            "unit": "ns",
            "range": "± 87.46309156457873"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_MultiSegment",
            "value": 17432.450876871746,
            "unit": "ns",
            "range": "± 494.95424899727846"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_ROM.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_MultiSegment.Allocated",
            "value": 112,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_ROM.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_MultiSegment.Allocated",
            "value": 4128,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_ROM.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_MultiSegment.Allocated",
            "value": 32808,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_ROM.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_MultiSegment.Allocated",
            "value": 112,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_ROM.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_MultiSegment.Allocated",
            "value": 4128,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_ROM.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_MultiSegment.Allocated",
            "value": 32808,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_Small.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_4K.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_32K.Allocated",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          }
        ]
      }
    ]
  }
}