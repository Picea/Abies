window.BENCHMARK_DATA = {
  "lastUpdate": 1770362314075,
  "repoUrl": "https://github.com/Picea/Abies",
  "entries": {
    "Virtual DOM Benchmarks": [
      {
        "commit": {
          "author": {
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters",
            "email": "MCGPPeters@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "bd568e31b00b62373fed10559b6a52906a7bc3bc",
          "message": "fix: Use string comparison for gh-pages existence check (#29)",
          "timestamp": "2026-02-05T17:47:10Z",
          "url": "https://github.com/Picea/Abies/commit/bd568e31b00b62373fed10559b6a52906a7bc3bc"
        },
        "date": 1770315430138,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.SmallDomDiff",
            "value": 690.0484898249308,
            "unit": "ns",
            "range": "± 2.1269517574730497"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.MediumDomDiff",
            "value": 827.6673535664876,
            "unit": "ns",
            "range": "± 8.439520430665128"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.LargeDomDiff",
            "value": 713.4371990839641,
            "unit": "ns",
            "range": "± 3.208940245449105"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.AttributeOnlyDiff",
            "value": 712.5139598846436,
            "unit": "ns",
            "range": "± 2.711803127107198"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.TextOnlyDiff",
            "value": 846.2599952697753,
            "unit": "ns",
            "range": "± 8.453522590224914"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeAdditionDiff",
            "value": 853.6884147644043,
            "unit": "ns",
            "range": "± 8.326568448249187"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeRemovalDiff",
            "value": 871.9484014511108,
            "unit": "ns",
            "range": "± 9.215764583280613"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "MCGPPeters@users.noreply.github.com",
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "33fd7a67d88ec3d49caf56aa6505908fffb79e13",
          "message": "perf: Reduce allocations in DOM diffing with object pooling (#30)\n\nThis PR tests the benchmark quality gates implemented in Issue #16.\n\nChanges:\n- Add object pooling for Dictionary, List<int>, and List<(int, int)>\n- Use ArrayPool<string> for key sequence arrays\n- Apply dotnet format (file-scoped namespace, braces)\n\nPerformance improvements:\n- ~21% faster execution time\n- ~75% reduction in memory allocations",
          "timestamp": "2026-02-05T21:03:29+01:00",
          "tree_id": "073e952b971ef6faf7859dfb31d26acec6ad2ab8",
          "url": "https://github.com/Picea/Abies/commit/33fd7a67d88ec3d49caf56aa6505908fffb79e13"
        },
        "date": 1770321991321,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.SmallDomDiff",
            "value": 566.0157080377851,
            "unit": "ns",
            "range": "± 2.095411636773913"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.MediumDomDiff",
            "value": 683.8945981539213,
            "unit": "ns",
            "range": "± 1.6036727090220502"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.LargeDomDiff",
            "value": 606.1284148876483,
            "unit": "ns",
            "range": "± 4.844955050735395"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.AttributeOnlyDiff",
            "value": 640.7683041436331,
            "unit": "ns",
            "range": "± 3.0848074105405874"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.TextOnlyDiff",
            "value": 715.9568350131696,
            "unit": "ns",
            "range": "± 3.4371719387168707"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeAdditionDiff",
            "value": 694.1961175373623,
            "unit": "ns",
            "range": "± 2.2242165766252633"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeRemovalDiff",
            "value": 708.8360641343253,
            "unit": "ns",
            "range": "± 10.444476873207885"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "MCGPPeters@users.noreply.github.com",
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "ca386a0c66bd5355ec420092b2ad2274de8be88b",
          "message": "feat: Add memory allocation tracking to benchmark dashboard (#31)\n\nAdd a second benchmark chart set 'Virtual DOM Allocations' that tracks\nBytesAllocatedPerOperation from the MemoryDiagnoser.\n\nChanges:\n- Add scripts/extract-allocations.py to parse BenchmarkDotNet JSON\n- Add allocation comparison and storage workflow steps\n- Use customSmallerIsBetter tool for allocation metrics\n- Stricter thresholds for allocations (120% alert, 150% fail)",
          "timestamp": "2026-02-05T21:24:12+01:00",
          "tree_id": "ffb124c7ee4615e0830ef5e86b89775577ed9126",
          "url": "https://github.com/Picea/Abies/commit/ca386a0c66bd5355ec420092b2ad2274de8be88b"
        },
        "date": 1770323232723,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.SmallDomDiff",
            "value": 570.7696338066688,
            "unit": "ns",
            "range": "± 2.0812336080607627"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.MediumDomDiff",
            "value": 682.9297353426615,
            "unit": "ns",
            "range": "± 2.442605415036218"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.LargeDomDiff",
            "value": 595.3033930460612,
            "unit": "ns",
            "range": "± 2.1382840967788694"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.AttributeOnlyDiff",
            "value": 620.555425303323,
            "unit": "ns",
            "range": "± 2.853040902565047"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.TextOnlyDiff",
            "value": 693.2933563232422,
            "unit": "ns",
            "range": "± 7.428451822471245"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeAdditionDiff",
            "value": 671.8841902869088,
            "unit": "ns",
            "range": "± 1.7197980934957824"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeRemovalDiff",
            "value": 673.4970641502968,
            "unit": "ns",
            "range": "± 0.6947657707367317"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "MCGPPeters@users.noreply.github.com",
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1e3841179ba068b4110a0f1de4b971135de1318d",
          "message": "perf: Toub-inspired optimizations for DOM operations (#33)\n\nApply techniques from Stephen Toub's .NET performance articles:\n\n1. Index String Cache (256 entries) - eliminates string interpolation for non-keyed child indices\n2. StringBuilder Pooling - reduces GC pressure during Apply operations  \n3. Append Chain Optimization - removes intermediate string allocations in RenderNode\n4. Refactored DiffChildrenCore - uses ReadOnlySpan<string> for key comparisons",
          "timestamp": "2026-02-06T08:15:24+01:00",
          "tree_id": "47f51052c8de39f05977793abf1c18d0c1c55a0f",
          "url": "https://github.com/Picea/Abies/commit/1e3841179ba068b4110a0f1de4b971135de1318d"
        },
        "date": 1770362311369,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.SmallDomDiff",
            "value": 549.3092784200396,
            "unit": "ns",
            "range": "± 2.5965684691926736"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.MediumDomDiff",
            "value": 667.4456221262614,
            "unit": "ns",
            "range": "± 2.4899871614242652"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.LargeDomDiff",
            "value": 574.8718249638875,
            "unit": "ns",
            "range": "± 3.330990220660288"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.AttributeOnlyDiff",
            "value": 611.5299082535964,
            "unit": "ns",
            "range": "± 2.3981030807681205"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.TextOnlyDiff",
            "value": 650.0044751485188,
            "unit": "ns",
            "range": "± 1.886963184505986"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeAdditionDiff",
            "value": 689.1478022795457,
            "unit": "ns",
            "range": "± 2.4698165682266353"
          },
          {
            "name": "Abies.Benchmarks.DomDiffingBenchmarks.NodeRemovalDiff",
            "value": 665.7882746378581,
            "unit": "ns",
            "range": "± 1.4550259067139961"
          }
        ]
      }
    ],
    "Virtual DOM Allocations": [
      {
        "commit": {
          "author": {
            "email": "MCGPPeters@users.noreply.github.com",
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "ca386a0c66bd5355ec420092b2ad2274de8be88b",
          "message": "feat: Add memory allocation tracking to benchmark dashboard (#31)\n\nAdd a second benchmark chart set 'Virtual DOM Allocations' that tracks\nBytesAllocatedPerOperation from the MemoryDiagnoser.\n\nChanges:\n- Add scripts/extract-allocations.py to parse BenchmarkDotNet JSON\n- Add allocation comparison and storage workflow steps\n- Use customSmallerIsBetter tool for allocation metrics\n- Stricter thresholds for allocations (120% alert, 150% fail)",
          "timestamp": "2026-02-05T21:24:12+01:00",
          "tree_id": "ffb124c7ee4615e0830ef5e86b89775577ed9126",
          "url": "https://github.com/Picea/Abies/commit/ca386a0c66bd5355ec420092b2ad2274de8be88b"
        },
        "date": 1770323233858,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "SmallDomDiff",
            "value": 312,
            "unit": "bytes",
            "extra": "Gen0: 19"
          },
          {
            "name": "MediumDomDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24"
          },
          {
            "name": "LargeDomDiff",
            "value": 344,
            "unit": "bytes",
            "extra": "Gen0: 21"
          },
          {
            "name": "AttributeOnlyDiff",
            "value": 360,
            "unit": "bytes",
            "extra": "Gen0: 22"
          },
          {
            "name": "TextOnlyDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24"
          },
          {
            "name": "NodeAdditionDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26"
          },
          {
            "name": "NodeRemovalDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "MCGPPeters@users.noreply.github.com",
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1e3841179ba068b4110a0f1de4b971135de1318d",
          "message": "perf: Toub-inspired optimizations for DOM operations (#33)\n\nApply techniques from Stephen Toub's .NET performance articles:\n\n1. Index String Cache (256 entries) - eliminates string interpolation for non-keyed child indices\n2. StringBuilder Pooling - reduces GC pressure during Apply operations  \n3. Append Chain Optimization - removes intermediate string allocations in RenderNode\n4. Refactored DiffChildrenCore - uses ReadOnlySpan<string> for key comparisons",
          "timestamp": "2026-02-06T08:15:24+01:00",
          "tree_id": "47f51052c8de39f05977793abf1c18d0c1c55a0f",
          "url": "https://github.com/Picea/Abies/commit/1e3841179ba068b4110a0f1de4b971135de1318d"
        },
        "date": 1770362313312,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "SmallDomDiff",
            "value": 312,
            "unit": "bytes",
            "extra": "Gen0: 19"
          },
          {
            "name": "MediumDomDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24"
          },
          {
            "name": "LargeDomDiff",
            "value": 344,
            "unit": "bytes",
            "extra": "Gen0: 21"
          },
          {
            "name": "AttributeOnlyDiff",
            "value": 360,
            "unit": "bytes",
            "extra": "Gen0: 22"
          },
          {
            "name": "TextOnlyDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24"
          },
          {
            "name": "NodeAdditionDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26"
          },
          {
            "name": "NodeRemovalDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26"
          }
        ]
      }
    ]
  }
}