window.BENCHMARK_DATA = {
  "lastUpdate": 1770323233132,
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
      }
    ]
  }
}