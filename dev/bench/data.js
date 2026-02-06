window.BENCHMARK_DATA = {
  "lastUpdate": 1770394866862,
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
    ],
    "Rendering Engine Throughput": [
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
          "id": "7bd08a22e214bbbffba2f824c2dd1b04e1415ea4",
          "message": "perf: Toub-inspired performance optimizations for DOM rendering (#34)\n\n* chore: trigger CI rerun\n\n* perf: Toub-inspired performance optimizations\n\nThree key optimizations inspired by Stephen Toub's .NET performance articles:\n\n1. Atomic counter for CommandIds (Events.cs)\n   - Replaced Guid.NewGuid().ToString() with atomic long counter\n   - Uses string.Create with stackalloc for zero-allocation ID generation\n   - Result: 10.9x faster handler creation (209ns → 19ns)\n   - Result: 21% less memory per handler (224B → 176B)\n\n2. SearchValues<char> fast-path for HTML encoding (Operations.cs)\n   - Uses SIMD-accelerated character search to skip HtmlEncode\n   - Most attribute values (class names, IDs) don't need encoding\n   - Result: 8% faster large page rendering\n\n3. FrozenDictionary cache for event attribute names (Operations.cs)\n   - Caches 'data-event-{name}' strings for 100+ known DOM events\n   - O(1) lookup eliminates string interpolation per handler\n   - Result: Additional 32% memory reduction per handler\n\nCombined results for event-heavy rendering:\n- 18% faster throughput\n- 34% less memory allocation\n\nBenchmark suite added:\n- RenderingBenchmarks.cs: 9 HTML rendering scenarios\n- EventHandlerBenchmarks.cs: 8 handler creation scenarios\n- CI quality gates: throughput (105%/110%), allocations (110%/120%)\n\n* fix: Address Copilot review feedback\n\n- Add missing scripts/merge-benchmark-results.py (was untracked)\n- Reduce stackalloc buffer from 32 to 24 chars (sufficient for max long)\n- Add overflow documentation comment explaining 292 million years to overflow\n- Update EventHandlerBenchmarks docs to reflect optimized implementation\n- Update RenderingBenchmarks docs to reflect SearchValues/counter optimizations\n\n* docs: Add memory instructions for PR template reminder\n\n* style: Add PR guidelines and fix formatting issues",
          "timestamp": "2026-02-06T10:36:53+01:00",
          "tree_id": "8565b6d5667d57fd15827ebda517d6ecd155ce55",
          "url": "https://github.com/Picea/Abies/commit/7bd08a22e214bbbffba2f824c2dd1b04e1415ea4"
        },
        "date": 1770371180205,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 555.7281339009603,
            "unit": "ns",
            "range": "± 1.9497550504789953"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 670.8262973785401,
            "unit": "ns",
            "range": "± 1.7592375689478408"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 597.9089409964425,
            "unit": "ns",
            "range": "± 1.4740506616804985"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 622.5981272969927,
            "unit": "ns",
            "range": "± 1.7708175213981892"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 694.6467865535191,
            "unit": "ns",
            "range": "± 0.8154460716081774"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 716.290855884552,
            "unit": "ns",
            "range": "± 1.7604849291788016"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 671.4078981399537,
            "unit": "ns",
            "range": "± 4.022456513733462"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 224.64049735864003,
            "unit": "ns",
            "range": "± 2.5559205820011104"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 819.5202875137329,
            "unit": "ns",
            "range": "± 3.358725072563177"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 402.3255453450339,
            "unit": "ns",
            "range": "± 2.1193160573673584"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 716.2043219975063,
            "unit": "ns",
            "range": "± 4.31917860297944"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5456.150481160482,
            "unit": "ns",
            "range": "± 91.30801250335917"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 39837.81648908342,
            "unit": "ns",
            "range": "± 267.4293304480751"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 655.533565725599,
            "unit": "ns",
            "range": "± 2.524106998529153"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 5023.290708688589,
            "unit": "ns",
            "range": "± 25.699557833313026"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2648.9821621821475,
            "unit": "ns",
            "range": "± 20.150211680087345"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 44.023834311962126,
            "unit": "ns",
            "range": "± 0.2544390758648351"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 52.74222540855408,
            "unit": "ns",
            "range": "± 0.22953492892673852"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 515.3010895068829,
            "unit": "ns",
            "range": "± 1.6702619803903755"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2507.768822303185,
            "unit": "ns",
            "range": "± 21.41229077728757"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4237.10751953125,
            "unit": "ns",
            "range": "± 34.45758758952155"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 100.46615355014801,
            "unit": "ns",
            "range": "± 1.6354668932731107"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 262.3463038444519,
            "unit": "ns",
            "range": "± 3.946396193311334"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 688.9725264231364,
            "unit": "ns",
            "range": "± 7.541682895800619"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7192.500771658762,
            "unit": "ns",
            "range": "± 73.32073629283512"
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
          "id": "3abb4a14a3d47f328427b1d8c9992b372a1fc551",
          "message": "refactor: Replace reflection-based JSON with source-generated serialization (#38)\n\n* refactor: replace reflection-based JSON with source-generated serialization\n\n.NET 10 WASM disables reflection-based JSON serialization by default\n(JsonSerializerIsReflectionDisabled). This commit adds source-generated\nJsonSerializerContext implementations for trim-safe serialization.\n\nFramework changes (Abies):\n- Add AbiesJsonContext for event/subscription data types\n- Update Runtime.DispatchData/DispatchSubscriptionData to use AbiesJsonContext\n- Update WebSocket subscription serialization to use AbiesJsonContext\n\nConduit app changes:\n- Add ConduitJsonContext with CamelCase naming for API types\n- Add explicit RequestDtos (LoginRequest, RegisterRequest, etc.) replacing\n  anonymous types that cannot be registered with source generators\n- Update ApiClient to use ConduitJsonContext and proper IDisposable disposal\n- Replace PublishTrimmed=false with Release-only trim settings\n\nTemplate changes:\n- Update both template .csproj files with Release-only trim settings\n\n* fix: Resolve CI failures for source-gen JSON PR\n\n- Fix Directory.Build.props: use $(MSBuildThisFileDirectory) instead of\n  $(SolutionDir) so Global/Usings.cs resolves in project-level builds\n  (fixes Benchmark and E2E CI failures)\n- Extract DispatchData/DispatchSubscriptionData into Runtime.Dispatch.cs\n  partial class to isolate source-gen JSON changes\n- Auto-format Runtime.cs to pass dotnet format lint check (fixes\n  pre-existing whitespace, unused usings, name simplifications)",
          "timestamp": "2026-02-06T17:11:12+01:00",
          "tree_id": "b5fd82a4b474f667d4afcd06a73b59f749724637",
          "url": "https://github.com/Picea/Abies/commit/3abb4a14a3d47f328427b1d8c9992b372a1fc551"
        },
        "date": 1770394864344,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 548.6162942886352,
            "unit": "ns",
            "range": "± 1.4458559735847722"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 668.9515376772199,
            "unit": "ns",
            "range": "± 2.136853869023948"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 591.8638168062482,
            "unit": "ns",
            "range": "± 1.6761242646769188"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 606.2203289667765,
            "unit": "ns",
            "range": "± 2.244381368555199"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 665.5970143590655,
            "unit": "ns",
            "range": "± 1.5931813214340595"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 684.2934725625174,
            "unit": "ns",
            "range": "± 2.913239318628167"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 667.1232634271894,
            "unit": "ns",
            "range": "± 2.843157142829055"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 217.09107943943567,
            "unit": "ns",
            "range": "± 0.8836970968781308"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 759.7578601837158,
            "unit": "ns",
            "range": "± 5.744130005443161"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 398.1805159886678,
            "unit": "ns",
            "range": "± 3.6730990151196323"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 718.7859888076782,
            "unit": "ns",
            "range": "± 3.1389304132930986"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5305.8632736206055,
            "unit": "ns",
            "range": "± 63.505227727324176"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 39290.707548014325,
            "unit": "ns",
            "range": "± 589.5096523223422"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 646.8601661046346,
            "unit": "ns",
            "range": "± 6.80517164333841"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4894.488583700998,
            "unit": "ns",
            "range": "± 35.73112573289629"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2482.9190015157064,
            "unit": "ns",
            "range": "± 31.41665796241797"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 39.32897735937782,
            "unit": "ns",
            "range": "± 1.0615102080983563"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 51.839095095793404,
            "unit": "ns",
            "range": "± 0.5560498708567455"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 502.82245787867794,
            "unit": "ns",
            "range": "± 13.945267347132093"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2568.5375073750815,
            "unit": "ns",
            "range": "± 42.20675892717792"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4091.1201805114747,
            "unit": "ns",
            "range": "± 90.61010161211068"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 95.49634311749385,
            "unit": "ns",
            "range": "± 2.688923508057292"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 248.03795177595956,
            "unit": "ns",
            "range": "± 3.1561806092355984"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 637.7872231801351,
            "unit": "ns",
            "range": "± 11.807842070170969"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7098.128535930927,
            "unit": "ns",
            "range": "± 107.42483623612995"
          }
        ]
      }
    ],
    "Rendering Engine Allocations": [
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
          "id": "7bd08a22e214bbbffba2f824c2dd1b04e1415ea4",
          "message": "perf: Toub-inspired performance optimizations for DOM rendering (#34)\n\n* chore: trigger CI rerun\n\n* perf: Toub-inspired performance optimizations\n\nThree key optimizations inspired by Stephen Toub's .NET performance articles:\n\n1. Atomic counter for CommandIds (Events.cs)\n   - Replaced Guid.NewGuid().ToString() with atomic long counter\n   - Uses string.Create with stackalloc for zero-allocation ID generation\n   - Result: 10.9x faster handler creation (209ns → 19ns)\n   - Result: 21% less memory per handler (224B → 176B)\n\n2. SearchValues<char> fast-path for HTML encoding (Operations.cs)\n   - Uses SIMD-accelerated character search to skip HtmlEncode\n   - Most attribute values (class names, IDs) don't need encoding\n   - Result: 8% faster large page rendering\n\n3. FrozenDictionary cache for event attribute names (Operations.cs)\n   - Caches 'data-event-{name}' strings for 100+ known DOM events\n   - O(1) lookup eliminates string interpolation per handler\n   - Result: Additional 32% memory reduction per handler\n\nCombined results for event-heavy rendering:\n- 18% faster throughput\n- 34% less memory allocation\n\nBenchmark suite added:\n- RenderingBenchmarks.cs: 9 HTML rendering scenarios\n- EventHandlerBenchmarks.cs: 8 handler creation scenarios\n- CI quality gates: throughput (105%/110%), allocations (110%/120%)\n\n* fix: Address Copilot review feedback\n\n- Add missing scripts/merge-benchmark-results.py (was untracked)\n- Reduce stackalloc buffer from 32 to 24 chars (sufficient for max long)\n- Add overflow documentation comment explaining 292 million years to overflow\n- Update EventHandlerBenchmarks docs to reflect optimized implementation\n- Update RenderingBenchmarks docs to reflect SearchValues/counter optimizations\n\n* docs: Add memory instructions for PR template reminder\n\n* style: Add PR guidelines and fix formatting issues",
          "timestamp": "2026-02-06T10:36:53+01:00",
          "tree_id": "8565b6d5667d57fd15827ebda517d6ecd155ce55",
          "url": "https://github.com/Picea/Abies/commit/7bd08a22e214bbbffba2f824c2dd1b04e1415ea4"
        },
        "date": 1770371181465,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 312,
            "unit": "bytes",
            "extra": "Gen0: 19.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 344,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 360,
            "unit": "bytes",
            "extra": "Gen0: 22.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 352,
            "unit": "bytes",
            "extra": "Gen0: 88.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1416,
            "unit": "bytes",
            "extra": "Gen0: 88.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 800,
            "unit": "bytes",
            "extra": "Gen0: 100.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1248,
            "unit": "bytes",
            "extra": "Gen0: 78.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9968,
            "unit": "bytes",
            "extra": "Gen0: 78.0000"
          },
          {
            "name": "Rendering/RenderLargePage",
            "value": 150176,
            "unit": "bytes",
            "extra": "Gen0: 146.0000, Gen1: 36.0000"
          },
          {
            "name": "Rendering/RenderDeeplyNested",
            "value": 1224,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
          },
          {
            "name": "Rendering/RenderWideTree",
            "value": 9400,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 5000,
            "unit": "bytes",
            "extra": "Gen0: 78.0000"
          },
          {
            "name": "Handlers/CreateSingleHandler_Message",
            "value": 120,
            "unit": "bytes",
            "extra": "Gen0: 120.0000"
          },
          {
            "name": "Handlers/CreateSingleHandler_Factory",
            "value": 208,
            "unit": "bytes",
            "extra": "Gen0: 208.0000"
          },
          {
            "name": "Handlers/Create10Handlers",
            "value": 1656,
            "unit": "bytes",
            "extra": "Gen0: 103.0000"
          },
          {
            "name": "Handlers/Create50Handlers",
            "value": 8184,
            "unit": "bytes",
            "extra": "Gen0: 128.0000, Gen1: 3.0000"
          },
          {
            "name": "Handlers/Create100Handlers",
            "value": 12824,
            "unit": "bytes",
            "extra": "Gen0: 100.0000, Gen1: 4.0000"
          },
          {
            "name": "Handlers/CreateButtonWithHandler",
            "value": 400,
            "unit": "bytes",
            "extra": "Gen0: 200.0000"
          },
          {
            "name": "Handlers/CreateInputWithMultipleHandlers",
            "value": 976,
            "unit": "bytes",
            "extra": "Gen0: 122.0000"
          },
          {
            "name": "Handlers/CreateFormWithHandlers",
            "value": 2424,
            "unit": "bytes",
            "extra": "Gen0: 151.0000, Gen1: 1.0000"
          },
          {
            "name": "Handlers/CreateArticleListWithHandlers",
            "value": 24104,
            "unit": "bytes",
            "extra": "Gen0: 188.0000, Gen1: 14.0000"
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
          "id": "3abb4a14a3d47f328427b1d8c9992b372a1fc551",
          "message": "refactor: Replace reflection-based JSON with source-generated serialization (#38)\n\n* refactor: replace reflection-based JSON with source-generated serialization\n\n.NET 10 WASM disables reflection-based JSON serialization by default\n(JsonSerializerIsReflectionDisabled). This commit adds source-generated\nJsonSerializerContext implementations for trim-safe serialization.\n\nFramework changes (Abies):\n- Add AbiesJsonContext for event/subscription data types\n- Update Runtime.DispatchData/DispatchSubscriptionData to use AbiesJsonContext\n- Update WebSocket subscription serialization to use AbiesJsonContext\n\nConduit app changes:\n- Add ConduitJsonContext with CamelCase naming for API types\n- Add explicit RequestDtos (LoginRequest, RegisterRequest, etc.) replacing\n  anonymous types that cannot be registered with source generators\n- Update ApiClient to use ConduitJsonContext and proper IDisposable disposal\n- Replace PublishTrimmed=false with Release-only trim settings\n\nTemplate changes:\n- Update both template .csproj files with Release-only trim settings\n\n* fix: Resolve CI failures for source-gen JSON PR\n\n- Fix Directory.Build.props: use $(MSBuildThisFileDirectory) instead of\n  $(SolutionDir) so Global/Usings.cs resolves in project-level builds\n  (fixes Benchmark and E2E CI failures)\n- Extract DispatchData/DispatchSubscriptionData into Runtime.Dispatch.cs\n  partial class to isolate source-gen JSON changes\n- Auto-format Runtime.cs to pass dotnet format lint check (fixes\n  pre-existing whitespace, unused usings, name simplifications)",
          "timestamp": "2026-02-06T17:11:12+01:00",
          "tree_id": "b5fd82a4b474f667d4afcd06a73b59f749724637",
          "url": "https://github.com/Picea/Abies/commit/3abb4a14a3d47f328427b1d8c9992b372a1fc551"
        },
        "date": 1770394866206,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 312,
            "unit": "bytes",
            "extra": "Gen0: 19.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 344,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 360,
            "unit": "bytes",
            "extra": "Gen0: 22.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 384,
            "unit": "bytes",
            "extra": "Gen0: 24.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 424,
            "unit": "bytes",
            "extra": "Gen0: 26.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 352,
            "unit": "bytes",
            "extra": "Gen0: 88.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1416,
            "unit": "bytes",
            "extra": "Gen0: 88.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 800,
            "unit": "bytes",
            "extra": "Gen0: 100.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1248,
            "unit": "bytes",
            "extra": "Gen0: 78.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9968,
            "unit": "bytes",
            "extra": "Gen0: 78.0000"
          },
          {
            "name": "Rendering/RenderLargePage",
            "value": 150176,
            "unit": "bytes",
            "extra": "Gen0: 146.0000, Gen1: 36.0000"
          },
          {
            "name": "Rendering/RenderDeeplyNested",
            "value": 1224,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
          },
          {
            "name": "Rendering/RenderWideTree",
            "value": 9400,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 5000,
            "unit": "bytes",
            "extra": "Gen0: 78.0000"
          },
          {
            "name": "Handlers/CreateSingleHandler_Message",
            "value": 120,
            "unit": "bytes",
            "extra": "Gen0: 120.0000"
          },
          {
            "name": "Handlers/CreateSingleHandler_Factory",
            "value": 208,
            "unit": "bytes",
            "extra": "Gen0: 208.0000"
          },
          {
            "name": "Handlers/Create10Handlers",
            "value": 1656,
            "unit": "bytes",
            "extra": "Gen0: 103.0000"
          },
          {
            "name": "Handlers/Create50Handlers",
            "value": 8184,
            "unit": "bytes",
            "extra": "Gen0: 128.0000, Gen1: 3.0000"
          },
          {
            "name": "Handlers/Create100Handlers",
            "value": 12824,
            "unit": "bytes",
            "extra": "Gen0: 100.0000, Gen1: 4.0000"
          },
          {
            "name": "Handlers/CreateButtonWithHandler",
            "value": 400,
            "unit": "bytes",
            "extra": "Gen0: 200.0000"
          },
          {
            "name": "Handlers/CreateInputWithMultipleHandlers",
            "value": 976,
            "unit": "bytes",
            "extra": "Gen0: 122.0000"
          },
          {
            "name": "Handlers/CreateFormWithHandlers",
            "value": 2424,
            "unit": "bytes",
            "extra": "Gen0: 151.0000, Gen1: 1.0000"
          },
          {
            "name": "Handlers/CreateArticleListWithHandlers",
            "value": 24344,
            "unit": "bytes",
            "extra": "Gen0: 190.0000, Gen1: 14.0000"
          }
        ]
      }
    ]
  }
}