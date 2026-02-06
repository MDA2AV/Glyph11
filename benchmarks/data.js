window.BENCHMARK_DATA = {
  "lastUpdate": 1770402649379,
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
      },
      {
        "commit": {
          "author": {
            "name": "Diogo Martins",
            "username": "MDA2AV",
            "email": "165835485+MDA2AV@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "c4e910db7d24ff2eec0a2560797e715f0f4d347b",
          "message": "trigger new hash",
          "timestamp": "2026-02-06T16:42:14Z",
          "url": "https://github.com/MDA2AV/Glyph11/commit/c4e910db7d24ff2eec0a2560797e715f0f4d347b"
        },
        "date": 1770396510705,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_Small",
            "value": 123.6187178293864,
            "unit": "ns",
            "range": "± 0.6136074696515343"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_4K",
            "value": 330.39487679799396,
            "unit": "ns",
            "range": "± 2.6939523017442086"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_32K",
            "value": 2343.1038716634116,
            "unit": "ns",
            "range": "± 1.0555558456052263"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_ROM",
            "value": 136.24384562174478,
            "unit": "ns",
            "range": "± 0.6493602907869944"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_MultiSegment",
            "value": 342.4900673230489,
            "unit": "ns",
            "range": "± 1.8952959213977278"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_ROM",
            "value": 724.3076359430949,
            "unit": "ns",
            "range": "± 0.26433953507641195"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_MultiSegment",
            "value": 1852.2872982025146,
            "unit": "ns",
            "range": "± 24.637860217879236"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_ROM",
            "value": 4659.83821105957,
            "unit": "ns",
            "range": "± 33.481471636611985"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_MultiSegment",
            "value": 12753.39974975586,
            "unit": "ns",
            "range": "± 9.743963203451218"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_ROM",
            "value": 180.72594849268594,
            "unit": "ns",
            "range": "± 0.29057780059984817"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_MultiSegment",
            "value": 433.06406116485596,
            "unit": "ns",
            "range": "± 0.8353578508904677"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_ROM",
            "value": 1064.837876001994,
            "unit": "ns",
            "range": "± 0.25327521089109006"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_MultiSegment",
            "value": 2240.535410563151,
            "unit": "ns",
            "range": "± 5.878816439723812"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_ROM",
            "value": 7134.643239339192,
            "unit": "ns",
            "range": "± 6.079048646679979"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_MultiSegment",
            "value": 16270.473876953125,
            "unit": "ns",
            "range": "± 71.65121261139609"
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
      },
      {
        "commit": {
          "author": {
            "name": "Diogo Martins",
            "username": "MDA2AV",
            "email": "165835485+MDA2AV@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "2b8f47e314dde96d5c73c65e1a60148c09e2c4cb",
          "message": "Merge pull request #16 from MDA2AV/move-bench\n\nMove benchmarks to root",
          "timestamp": "2026-02-06T16:59:56Z",
          "url": "https://github.com/MDA2AV/Glyph11/commit/2b8f47e314dde96d5c73c65e1a60148c09e2c4cb"
        },
        "date": 1770397457320,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_Small",
            "value": 125.9111939271291,
            "unit": "ns",
            "range": "± 1.0575460767138234"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_4K",
            "value": 338.01439301172894,
            "unit": "ns",
            "range": "± 1.5740496070739196"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_32K",
            "value": 2344.38224029541,
            "unit": "ns",
            "range": "± 2.7448174101359797"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_ROM",
            "value": 137.1301510334015,
            "unit": "ns",
            "range": "± 1.189542591262213"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_MultiSegment",
            "value": 346.5772439638774,
            "unit": "ns",
            "range": "± 1.435360911617415"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_ROM",
            "value": 675.3335173924764,
            "unit": "ns",
            "range": "± 0.7766832860409361"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_MultiSegment",
            "value": 1728.8943411509197,
            "unit": "ns",
            "range": "± 4.533599991113948"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_ROM",
            "value": 4505.650652567546,
            "unit": "ns",
            "range": "± 24.923738102538287"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_MultiSegment",
            "value": 11705.23560587565,
            "unit": "ns",
            "range": "± 19.593770579077045"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_ROM",
            "value": 182.79303153355917,
            "unit": "ns",
            "range": "± 1.0643140222178733"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_MultiSegment",
            "value": 416.2855900128682,
            "unit": "ns",
            "range": "± 2.079198145613925"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_ROM",
            "value": 1056.3842525482178,
            "unit": "ns",
            "range": "± 1.907673237645705"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_MultiSegment",
            "value": 2089.9739138285317,
            "unit": "ns",
            "range": "± 6.221251776185811"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_ROM",
            "value": 7224.245206197103,
            "unit": "ns",
            "range": "± 8.550585393205699"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_MultiSegment",
            "value": 14821.573379516602,
            "unit": "ns",
            "range": "± 54.11375937213927"
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
      },
      {
        "commit": {
          "author": {
            "name": "Diogo Martins",
            "username": "MDA2AV",
            "email": "165835485+MDA2AV@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "8247b3ba5ca483264e4d90878da6e3d4b0906cc3",
          "message": "Merge pull request #18 from MDA2AV/glyph-probe\n\nCreate Glyph Probe",
          "timestamp": "2026-02-06T18:24:50Z",
          "url": "https://github.com/MDA2AV/Glyph11/commit/8247b3ba5ca483264e4d90878da6e3d4b0906cc3"
        },
        "date": 1770402648806,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_Small",
            "value": 125.1464437643687,
            "unit": "ns",
            "range": "± 0.8615978858596918"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_4K",
            "value": 330.2873498598735,
            "unit": "ns",
            "range": "± 2.2807762805639324"
          },
          {
            "name": "Benchmarks.AllSemanticChecksBenchmark.AllChecks_32K",
            "value": 2346.685577392578,
            "unit": "ns",
            "range": "± 0.02745951474181768"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_ROM",
            "value": 137.25885931650797,
            "unit": "ns",
            "range": "± 1.8368149414307962"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Small_MultiSegment",
            "value": 337.8675676981608,
            "unit": "ns",
            "range": "± 2.649229278115304"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_ROM",
            "value": 716.6137603123983,
            "unit": "ns",
            "range": "± 0.43652938680675074"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header4K_MultiSegment",
            "value": 1693.583651860555,
            "unit": "ns",
            "range": "± 6.097576343867055"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_ROM",
            "value": 4930.780570983887,
            "unit": "ns",
            "range": "± 17.656759241132352"
          },
          {
            "name": "Benchmarks.FlexibleParserBenchmark.Header32K_MultiSegment",
            "value": 11853.33356221517,
            "unit": "ns",
            "range": "± 134.6003134252227"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_ROM",
            "value": 183.5591255823771,
            "unit": "ns",
            "range": "± 0.09926299427632192"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Small_MultiSegment",
            "value": 443.5447591145833,
            "unit": "ns",
            "range": "± 1.6868685718393825"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_ROM",
            "value": 996.8340295155843,
            "unit": "ns",
            "range": "± 0.2718354991291682"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header4K_MultiSegment",
            "value": 2243.943861643473,
            "unit": "ns",
            "range": "± 29.680815082157288"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_ROM",
            "value": 7517.927272796631,
            "unit": "ns",
            "range": "± 94.71946858968279"
          },
          {
            "name": "Benchmarks.HardenedParserBenchmark.Header32K_MultiSegment",
            "value": 14605.815272013346,
            "unit": "ns",
            "range": "± 32.262597434911896"
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