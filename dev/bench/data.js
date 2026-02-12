window.BENCHMARK_DATA = {
  "lastUpdate": 1770910549209,
  "repoUrl": "https://github.com/Picea/Abies",
  "entries": {
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
          "id": "62c8dd894f28980360dae04ff68f54eb14b08199",
          "message": "perf: add startup performance benchmarks and E2E quality gates (#40)",
          "timestamp": "2026-02-06T17:30:09+01:00",
          "tree_id": "72d4ba0bf8d8ec8d16568309be1748fda28f3c67",
          "url": "https://github.com/Picea/Abies/commit/62c8dd894f28980360dae04ff68f54eb14b08199"
        },
        "date": 1770395983459,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 551.7758168492999,
            "unit": "ns",
            "range": "± 1.0444367751699615"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 672.7541401045663,
            "unit": "ns",
            "range": "± 1.0595674451094204"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 583.8268250147502,
            "unit": "ns",
            "range": "± 1.3901049887836348"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 610.3586098988851,
            "unit": "ns",
            "range": "± 3.0177638773277478"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 671.8154952709491,
            "unit": "ns",
            "range": "± 2.0758692424853673"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 683.7992922919137,
            "unit": "ns",
            "range": "± 1.3477645405733625"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 678.5452966690063,
            "unit": "ns",
            "range": "± 1.0896387567305073"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 216.42522032444293,
            "unit": "ns",
            "range": "± 1.0446117989447505"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 759.5094691594442,
            "unit": "ns",
            "range": "± 7.70091826326834"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 398.40349938074746,
            "unit": "ns",
            "range": "± 3.5080306837613344"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 703.3379477659861,
            "unit": "ns",
            "range": "± 1.6636394913284798"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5279.448818751744,
            "unit": "ns",
            "range": "± 20.824886507653193"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 38220.05347086589,
            "unit": "ns",
            "range": "± 271.9359620189377"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 627.2517081669399,
            "unit": "ns",
            "range": "± 3.6721452089528914"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4889.934798685709,
            "unit": "ns",
            "range": "± 35.62773461232112"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2480.0199478149416,
            "unit": "ns",
            "range": "± 12.480376450933504"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 38.721775270425354,
            "unit": "ns",
            "range": "± 0.2618549239591317"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 50.992101911987575,
            "unit": "ns",
            "range": "± 0.458368104820032"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 498.3341453552246,
            "unit": "ns",
            "range": "± 7.631719096263722"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2455.767603556315,
            "unit": "ns",
            "range": "± 23.83890113733969"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4027.1865310668945,
            "unit": "ns",
            "range": "± 29.38368785032931"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 95.88967414299647,
            "unit": "ns",
            "range": "± 0.8072928589846499"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 250.6280554930369,
            "unit": "ns",
            "range": "± 2.335775513676915"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 641.8849888483684,
            "unit": "ns",
            "range": "± 6.831889460637978"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7053.016693115234,
            "unit": "ns",
            "range": "± 73.37976953579846"
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
          "id": "82795013cf2457c431923e806bf00d242685a9b8",
          "message": "fix: Use appropriate container for parsing table-related HTML elements (#43)\n\n* docs: Document NETSDK1152 fix and performance trade-offs\n\nAdded comprehensive documentation for:\n- PR #38 source-gen JSON performance trade-off (10-20% handler creation regression)\n- NETSDK1152 duplicate abies.js fix with MSBuild target solution\n- Key insights about using Identity metadata vs OriginalItemSpec\n- Cross-platform path separator handling (Windows \\ vs Unix /)\n\n* fix: use appropriate container for parsing table-related HTML elements\n\nBrowsers strip table-related elements (tr, td, th, etc.) when placed inside\ninvalid container elements like <div>. This caused DOM patching operations\nto silently fail for table rows and cells.\n\nAdded parseHtmlFragment() helper that selects the correct container:\n- <tbody> for <tr> elements\n- <tr> for <td>/<th> elements\n- <table> for <thead>/<tbody>/<tfoot>/<colgroup>/<caption>\n- <colgroup> for <col> elements\n- <select> for <option>/<optgroup> elements\n- <div> for everything else (default)\n\nThis fixes issues with dynamic table content like js-framework-benchmark's\n'Create 1000 rows' operation where only <span> child content was rendered\nbut parent <tr>/<td>/<a> elements were stripped.\n\nFixes #32\n\n* chore: Trigger CI workflow\n\n* fix: Respect explicit id attributes in HTML element helpers\n\n- Modified element() function to check for user-provided id attributes\n- When an explicit id attribute is passed via Attributes.id(), use that value\n- Falls back to auto-generated ID when no explicit id is provided\n- Filters duplicate id attributes to avoid rendering id twice\n\nThis fix is required for js-framework-benchmark where buttons need\nspecific IDs like id=\"run\", id=\"add\", etc.\n\nAlso updated AbiesBenchmark.csproj:\n- Target .NET 10.0\n- Use local Abies project reference instead of NuGet package\n- Enable trimming with partial TrimMode",
          "timestamp": "2026-02-07T09:46:30+01:00",
          "tree_id": "24d7996e85bf43a8b5c50f3d3fd9a95ceb011e71",
          "url": "https://github.com/Picea/Abies/commit/82795013cf2457c431923e806bf00d242685a9b8"
        },
        "date": 1770454555796,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 504.8173532485962,
            "unit": "ns",
            "range": "± 1.3480709905633061"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3027.710081100464,
            "unit": "ns",
            "range": "± 4.462109444817293"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 527.987700843811,
            "unit": "ns",
            "range": "± 5.038111396037027"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 597.7805709203084,
            "unit": "ns",
            "range": "± 2.5200979839532547"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 618.8328320821126,
            "unit": "ns",
            "range": "± 2.3212671953783164"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 606.0251963479178,
            "unit": "ns",
            "range": "± 2.448338888428998"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 617.081737450191,
            "unit": "ns",
            "range": "± 2.392739019649222"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 193.3844588484083,
            "unit": "ns",
            "range": "± 1.7828252269009548"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 717.0477017084758,
            "unit": "ns",
            "range": "± 12.893252448163167"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 371.266581092562,
            "unit": "ns",
            "range": "± 3.8944017030622944"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 649.2982226053874,
            "unit": "ns",
            "range": "± 10.269585927506812"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5216.178850809733,
            "unit": "ns",
            "range": "± 60.698231060322115"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 36542.78922119141,
            "unit": "ns",
            "range": "± 143.62830259307142"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 619.1489818436759,
            "unit": "ns",
            "range": "± 2.3648209666613913"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4707.396107600285,
            "unit": "ns",
            "range": "± 13.47600664265144"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2330.674347468785,
            "unit": "ns",
            "range": "± 10.020826675282462"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 38.32052082094279,
            "unit": "ns",
            "range": "± 1.0085198519282477"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 48.67131296487955,
            "unit": "ns",
            "range": "± 0.16178149737063466"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 482.9869876274696,
            "unit": "ns",
            "range": "± 1.1634430441955368"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2242.226709638323,
            "unit": "ns",
            "range": "± 10.198526849327546"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 3936.5696291242325,
            "unit": "ns",
            "range": "± 26.341189927251833"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 99.24011612252185,
            "unit": "ns",
            "range": "± 2.280110855661222"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 274.677575969696,
            "unit": "ns",
            "range": "± 6.2315288276563825"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 640.6240836552212,
            "unit": "ns",
            "range": "± 20.08535026129269"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7223.051341247558,
            "unit": "ns",
            "range": "± 81.0292333291752"
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
          "id": "86f123a11af62392ed0db6c94a79d9fff9e026e1",
          "message": "perf: Batch DOM operations into single JS interop call (#48)\n\n* perf: batch DOM operations into single JS interop call\n\nReplaces individual JS interop calls per DOM operation with a single\nbatched call that applies all pending operations at once. This reduces\nthe overhead of crossing the WASM/JS boundary, especially for views\nwith many elements.\n\nChanges:\n- Add Operations.cs with JSON-serializable DOM operation types\n- Add applyPatch function in abies.js for batch processing\n- Update Interop.cs to use batched patching\n- Add PatchOperationArray to AbiesJsonContext for source generation\n- Update Runtime.cs to use new batched diffing\n\nThis optimization is particularly impactful for large lists and\ncomplex views where many DOM operations occur during a single update.\n\n* fix: address Copilot review feedback\n\n- Use parseHtmlFragment for AddChild instead of insertAdjacentHTML\n- Use addEventListeners(element) to register event handlers on inserted nodes\n- Add parentNode guard in RemoveChild before calling remove()\n- Sync textarea.value in UpdateText/UpdateTextWithId like updateTextContent\n- Remove unused variable assignment in RemoveHandler case (Operations.cs)\n\n* fix: align batched attribute handling with non-batched behavior\n\nBoolean attributes in UpdateAttribute/AddAttribute now use empty string\nvalues like the non-batched updateAttribute/addAttribute functions do.\nAlso reorder conditionals to check value first, then boolean attrs.\n\n* ci: Implement same-job benchmark comparison for accurate PR validation\n\n- Add scripts/compare-benchmarks.py for same-job comparison (no runner variance)\n- Update benchmark workflow to run both main and PR benchmarks in same job\n- Same-job comparison is the pass/fail gate (thresholds: 110% throughput, 120% allocs)\n- benchmark-action still runs but with fail-on-alert=false (for gh-pages trends only)\n- Add benchmark-results/ and benchmark-baseline/ to gitignore\n\nThis eliminates false positives caused by CI runner variance between separate jobs.\n\n* chore: sync abies.js copies and cleanup whitespace",
          "timestamp": "2026-02-08T15:47:48+01:00",
          "tree_id": "f12974c3f5e30ef8d7974caf7045727908e6000a",
          "url": "https://github.com/Picea/Abies/commit/86f123a11af62392ed0db6c94a79d9fff9e026e1"
        },
        "date": 1770562615204,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 490.66295924553503,
            "unit": "ns",
            "range": "± 1.1761437541118562"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3114.1089797386758,
            "unit": "ns",
            "range": "± 4.739181074462296"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 530.427319254194,
            "unit": "ns",
            "range": "± 1.7660321728918325"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 595.732923189799,
            "unit": "ns",
            "range": "± 0.8744220202283062"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 614.5384984383217,
            "unit": "ns",
            "range": "± 1.814201054466815"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 648.294807434082,
            "unit": "ns",
            "range": "± 1.3834395427984412"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 640.7674238840739,
            "unit": "ns",
            "range": "± 1.453039910709688"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 189.99634906450908,
            "unit": "ns",
            "range": "± 1.1965261680845871"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 739.4317644755046,
            "unit": "ns",
            "range": "± 7.06553921128894"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 371.73133131663,
            "unit": "ns",
            "range": "± 3.018394078551686"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 669.279162534078,
            "unit": "ns",
            "range": "± 4.488276216939302"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5253.145320129394,
            "unit": "ns",
            "range": "± 23.835434110634523"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 37163.35734340123,
            "unit": "ns",
            "range": "± 251.8670502949384"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 679.6114823023478,
            "unit": "ns",
            "range": "± 6.394772743864064"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4737.8218503679545,
            "unit": "ns",
            "range": "± 32.98443033181185"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2355.218610029954,
            "unit": "ns",
            "range": "± 13.183852990069827"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 38.92439101934433,
            "unit": "ns",
            "range": "± 0.5714832597795486"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 49.813121114458355,
            "unit": "ns",
            "range": "± 0.30937327837499573"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 507.15995059694563,
            "unit": "ns",
            "range": "± 4.038945901417226"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2333.866308085124,
            "unit": "ns",
            "range": "± 22.041665477934977"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4097.330620985765,
            "unit": "ns",
            "range": "± 24.953888507188516"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 105.60859640286519,
            "unit": "ns",
            "range": "± 0.42000317214950955"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 264.7753088633219,
            "unit": "ns",
            "range": "± 2.528147394248956"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 668.372051302592,
            "unit": "ns",
            "range": "± 12.47189179919835"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7403.217665536063,
            "unit": "ns",
            "range": "± 43.955577061801584"
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
          "id": "7850fba6ccfa61343e1960cbba08fb55ef60fe65",
          "message": "perf: Optimize swap rows with LIS-based minimal DOM moves (#50)\n\n* perf: Optimize swap rows with LIS algorithm\n\nImplement Longest Increasing Subsequence (LIS) algorithm for optimal\nDOM reordering during keyed diffing. This reduces DOM operations from\nO(n) remove+add pairs to O(k) moves where k = n - LIS length.\n\nChanges:\n- Add ComputeLIS() using O(n log n) binary search algorithm (from Inferno)\n- Add MoveChild patch type for efficient DOM repositioning via insertBefore\n- Update DiffChildrenCore to use LIS for identifying elements to move\n- Fix MoveChild to use OLD element IDs (elements exist in DOM before patch)\n- Add moveChild JS function and batch handler in abies.js\n- Add comprehensive unit tests for LIS edge cases\n- Update memory.instructions.md with js-framework-benchmark setup guide\n\nBenchmark results (05_swap1k):\n- Before: 2000 DOM ops (remove all + add all) - BROKEN\n- After: 2 DOM ops (MoveChild only) - 406.7ms median\n\nThe algorithm correctly identifies that swapping rows 1↔998 only requires\nmoving 2 elements, not rebuilding the entire list.\n\n* fix(e2e): Fix flaky EditArticle test by waiting for Update button visibility\n\nThe test was failing intermittently because it checked button enabled state\nbefore the article data finished loading. In some cases, the form still\nshowed \"Publish Article\" (new mode) instead of \"Update Article\" (edit mode).\n\nFix: Wait for \"Update Article\" button to be VISIBLE before modifying the\ntitle. This confirms the slug is loaded and we're in edit mode. Then wait\nfor ENABLED state after form modifications.\n\nFixes timeout in EditArticle_AuthorCanModify E2E test.\n\n* style(e2e): Fix formatting in ArticleTests.cs",
          "timestamp": "2026-02-09T10:50:57+01:00",
          "tree_id": "ed91d66281db5468977bc1e5e9471117afd81f15",
          "url": "https://github.com/Picea/Abies/commit/7850fba6ccfa61343e1960cbba08fb55ef60fe65"
        },
        "date": 1770631240142,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 494.8377917607625,
            "unit": "ns",
            "range": "± 1.110715084276751"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3024.475149888259,
            "unit": "ns",
            "range": "± 6.763991619456707"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 527.1427401029147,
            "unit": "ns",
            "range": "± 2.080621126147704"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 609.9081845650306,
            "unit": "ns",
            "range": "± 3.104478856117408"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 595.0028640747071,
            "unit": "ns",
            "range": "± 3.6200517754970094"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 632.6222932679312,
            "unit": "ns",
            "range": "± 2.659911225492679"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 638.203230197613,
            "unit": "ns",
            "range": "± 1.8183091409881447"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 188.5024595627418,
            "unit": "ns",
            "range": "± 1.6607748685067807"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 762.46257909139,
            "unit": "ns",
            "range": "± 5.897912717740722"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 374.74163958231605,
            "unit": "ns",
            "range": "± 3.541132819872819"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 659.2049060821533,
            "unit": "ns",
            "range": "± 7.949440181227854"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5208.106112343924,
            "unit": "ns",
            "range": "± 36.79862162535717"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 37403.954744466144,
            "unit": "ns",
            "range": "± 545.2191293548221"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 624.3969631195068,
            "unit": "ns",
            "range": "± 6.286728083690655"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4742.373024495443,
            "unit": "ns",
            "range": "± 41.969439536929656"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2418.852583058675,
            "unit": "ns",
            "range": "± 11.241812773825231"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 37.66785058577855,
            "unit": "ns",
            "range": "± 0.4770536017053965"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 51.93314459423224,
            "unit": "ns",
            "range": "± 0.26850733303560576"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 671.2604690551758,
            "unit": "ns",
            "range": "± 8.561952136761292"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2378.064096323649,
            "unit": "ns",
            "range": "± 36.185909715083255"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4259.307705402374,
            "unit": "ns",
            "range": "± 83.11689599039356"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 96.05685444978567,
            "unit": "ns",
            "range": "± 0.459946197623107"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 264.82489230082587,
            "unit": "ns",
            "range": "± 10.969793518469578"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 625.0770479348989,
            "unit": "ns",
            "range": "± 1.982414264642316"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7195.667129007975,
            "unit": "ns",
            "range": "± 40.96798125598442"
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
          "id": "3dd327e3e81f877d4d89f4068b377949c0bd4631",
          "message": "perf: Pre-register event listeners to avoid DOM scanning (#55)\n\n* perf: pre-register event listeners to avoid DOM scanning\n\n- Pre-register 60+ common event types at module load time\n- Skip O(n) querySelectorAll scanning for incremental updates\n- Only scan on full page render for custom event types\n- Use TreeWalker instead of querySelectorAll when scanning\n\nThis eliminates the addEventListeners() overhead on every DOM update,\nwhich was scanning all nodes to discover data-event-* attributes.\nNow that all common events are pre-registered, incremental updates\n(AddChild, ReplaceChild) skip scanning entirely.\n\n* fix: address review comments for event listener pre-registration\n\n- Move COMMON_EVENT_TYPES.forEach() after runMain() to avoid TDZ issue\n- Remove early return in addEventListeners() to preserve custom event discovery\n- Keep TreeWalker optimization for all scans (more memory efficient)\n- Include root element when scanning subtrees\n\nAddresses review comments from PR #55",
          "timestamp": "2026-02-09T12:34:04+01:00",
          "tree_id": "6ac2f43626391172162cb2c94f893be7c9501fee",
          "url": "https://github.com/Picea/Abies/commit/3dd327e3e81f877d4d89f4068b377949c0bd4631"
        },
        "date": 1770637390779,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 516.1197224396926,
            "unit": "ns",
            "range": "± 0.7810680992524329"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3026.671943664551,
            "unit": "ns",
            "range": "± 5.009381741504286"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 528.6520595550537,
            "unit": "ns",
            "range": "± 1.0446015906353392"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 586.2522364934285,
            "unit": "ns",
            "range": "± 0.896760478110384"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 621.513681778541,
            "unit": "ns",
            "range": "± 0.8180922522574419"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 611.9001653535025,
            "unit": "ns",
            "range": "± 1.9121062391794073"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 642.3872699737549,
            "unit": "ns",
            "range": "± 1.9645915003107155"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 158.46328608989717,
            "unit": "ns",
            "range": "± 1.706703075035911"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 671.9957122166951,
            "unit": "ns",
            "range": "± 5.73949328417667"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 374.8198870340983,
            "unit": "ns",
            "range": "± 2.472365869821845"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 562.0053981781006,
            "unit": "ns",
            "range": "± 3.3810367366598104"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 4250.59948018392,
            "unit": "ns",
            "range": "± 54.22786152924198"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 35797.87312469482,
            "unit": "ns",
            "range": "± 1264.8708965270082"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 650.6374517849514,
            "unit": "ns",
            "range": "± 2.037602000666829"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4008.324872153146,
            "unit": "ns",
            "range": "± 36.66617168614308"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2135.162490081787,
            "unit": "ns",
            "range": "± 22.054145850984792"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 44.70599835713704,
            "unit": "ns",
            "range": "± 0.651711884784018"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 56.89351244767507,
            "unit": "ns",
            "range": "± 1.2580694230788958"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 581.8757051467895,
            "unit": "ns",
            "range": "± 10.198572996538324"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2872.059964243571,
            "unit": "ns",
            "range": "± 39.60050490237717"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4978.395516967774,
            "unit": "ns",
            "range": "± 72.00084871904161"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 116.81305861473083,
            "unit": "ns",
            "range": "± 3.236781619780147"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 311.5309536457062,
            "unit": "ns",
            "range": "± 5.740699020801028"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 774.2382854734149,
            "unit": "ns",
            "range": "± 13.1811425404024"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 8676.406733194986,
            "unit": "ns",
            "range": "± 118.95251744206367"
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
          "id": "55acab27767ad13008b7368edbe4b09ade02531e",
          "message": "perf: Add lazy memoization for deferred node evaluation (#56)\n\n* perf: Add lazy memoization for deferred node evaluation\n\nImplements ILazyMemoNode and LazyMemo<TKey> types that defer node\nconstruction until actually needed during diffing. This provides\ntrue Elm-style lazy semantics where the factory function is only\ncalled when memo keys differ.\n\nKey changes:\n- Add ILazyMemoNode interface with MemoKey, CachedNode, Evaluate()\n- Add LazyMemo<TKey> record implementing ILazyMemoNode\n- Add lazy<TKey>() helper function in Elements.cs\n- Update DiffInternal to handle lazy memo nodes before regular memo\n- Add UnwrapMemoNode helper for consistent memo unwrapping\n- Add MemoHits/MemoMisses counters for performance analysis\n- Update Runtime.PreserveIds to handle lazy memo nodes\n- Add comprehensive unit tests for lazy memo behavior\n\nBenchmark results (Select 1k):\n- Before lazy: ~152.4ms median\n- With lazy: ~111.9ms median (27% improvement)\n\nThe lazy approach skips both node construction AND subtree diffing\nfor unchanged rows, providing significant performance gains for\nlist-heavy UIs like the js-framework-benchmark.\n\n* style: fix formatting in changed files",
          "timestamp": "2026-02-09T14:51:00+01:00",
          "tree_id": "37da48bcc527851433203be9c55785d3a0d9f5ce",
          "url": "https://github.com/Picea/Abies/commit/55acab27767ad13008b7368edbe4b09ade02531e"
        },
        "date": 1770645632463,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 516.3822961534772,
            "unit": "ns",
            "range": "± 2.3083624557176883"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3172.552852376302,
            "unit": "ns",
            "range": "± 4.240378809997411"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 546.1475593021938,
            "unit": "ns",
            "range": "± 1.045383337036525"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 600.6553619248526,
            "unit": "ns",
            "range": "± 1.5305187120338493"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 618.9486166000366,
            "unit": "ns",
            "range": "± 1.3311732626066313"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 629.7284515087421,
            "unit": "ns",
            "range": "± 1.4586270178706424"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 637.9224793116251,
            "unit": "ns",
            "range": "± 0.806746994585975"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 189.93025661431827,
            "unit": "ns",
            "range": "± 0.23889932589880733"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 723.695557814378,
            "unit": "ns",
            "range": "± 1.1437479096981347"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 356.7506052335103,
            "unit": "ns",
            "range": "± 1.0626009495585929"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 643.1650519688924,
            "unit": "ns",
            "range": "± 1.2616905720263538"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5112.868414815267,
            "unit": "ns",
            "range": "± 13.477006377608182"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 36734.53280874399,
            "unit": "ns",
            "range": "± 164.75342205060662"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 633.1469256877899,
            "unit": "ns",
            "range": "± 12.488029508005017"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4781.056743367513,
            "unit": "ns",
            "range": "± 69.86434550608145"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2446.3850881788467,
            "unit": "ns",
            "range": "± 50.362898178220775"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 37.55402879204069,
            "unit": "ns",
            "range": "± 0.11543827769009936"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 52.005844903843744,
            "unit": "ns",
            "range": "± 0.5412062281376641"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 491.1856894493103,
            "unit": "ns",
            "range": "± 3.5481985385820747"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2679.2093818664553,
            "unit": "ns",
            "range": "± 45.41987355585029"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4017.2135581970215,
            "unit": "ns",
            "range": "± 87.75360584972097"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 100.43297999501229,
            "unit": "ns",
            "range": "± 2.380265040149932"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 265.1171269076211,
            "unit": "ns",
            "range": "± 3.8237224436135073"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 645.9117093616062,
            "unit": "ns",
            "range": "± 13.492166341501687"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7264.1616543361115,
            "unit": "ns",
            "range": "± 65.7174154827236"
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
          "id": "671652a954493dcdaba4de53a2acf14f3116581c",
          "message": "perf: Defer OTel CDN loading to after first paint (#57)\n\n* perf: Defer OTel CDN loading to after first paint\n\nThis optimization improves First Paint performance by:\n\n1. Installing the lightweight OTel shim synchronously (no network dependency)\n2. Deferring CDN-based OTel SDK loading to requestIdleCallback/setTimeout\n3. Never blocking the critical path (dotnet.create() -> runMain())\n\nThe shim provides full tracing functionality during startup, and the\nCDN upgrade happens transparently in the background after first paint.\n\nKey changes:\n- Extract initLocalOtelShim() as a named synchronous function\n- Extract upgradeToFullOtel() as the async CDN loading function\n- Add scheduleDeferredOtelUpgrade() to run after app initialization\n- Remove the blocking async IIFE that ran at module load\n\nPerformance impact:\n- Before: ~4800ms First Paint (OTel CDN loading blocked startup)\n- After: ~100ms First Paint (OTel loads in background)\n\nFixes #3 in Performance Optimization Plan\n\n* fix: Unwrap memo nodes in MoveChild patch generation\n\nWhen generating MoveChild patches for keyed diffing, the code was comparing\nnode types without first unwrapping memo nodes. This caused incorrect type\ncomparisons when memoized elements were involved in reordering operations.\n\nAdded UnwrapMemoNode() calls before type checking to ensure we compare the\nactual underlying Element types, not the memo wrapper types.\n\n* ci: Increase benchmark threshold to 15% for CI variance\n\nCI runner benchmarks show up to 20% variance in confidence intervals due to:\n- GC timing differences between runs\n- Shared infrastructure resource contention\n- Complex benchmarks (larger allocations) showing more variance than simple ones\n\nIncreased threshold from 110% to 115% to reduce false positives while still\ncatching genuine regressions. Local benchmarks confirmed variance patterns:\n- CreateButtonWithHandler: ±20.30% CI\n- CreateInputWithMultipleHandlers: ±19.42% CI\n\n* perf: Defer OTel CDN loading to after first paint\n\nMoved OpenTelemetry SDK loading from blocking script execution to\nrequestIdleCallback (with setTimeout fallback). This ensures:\n- First paint is not blocked by CDN latency\n- OTel loads during browser idle time after initial render\n- Graceful degradation if CDN is slow or unavailable\n\nThe shim ensures all tracing calls work immediately, with real\nimplementation hydrated asynchronously after first paint.\n\n* fix: Address OTel review comments for PR #57\n\nReview fixes from copilot-pull-request-reviewer:\n\n1. Early return if isOtelDisabled in initLocalOtelShim() to respect\n   global disable switches and avoid unnecessary shim overhead\n\n2. Expanded fetch ignore condition to cover:\n   - OTLP proxy endpoint (/otlp/v1/traces)\n   - Common collector endpoints (/v1/traces)\n   - Custom configured exporter URL\n   - Blazor framework downloads (/_framework/)\n\n3. Restore original fetch before registering full OTel instrumentations\n   to prevent double-patching and context propagation issues\n\n4. Fix setVerbosity cache invalidation - both shim and full OTel now\n   call resetVerbosityCache() so runtime verbosity changes take effect\n\n5. Fix header guard that always evaluated to true (i && i.headers)\n\n* Cache Playwright browsers in CI workflow\n\nAdd caching for Playwright browsers to improve CI performance.",
          "timestamp": "2026-02-09T17:06:22+01:00",
          "tree_id": "e29d6aaddfb9ec0e0ab3aef0977276e7636af2c4",
          "url": "https://github.com/Picea/Abies/commit/671652a954493dcdaba4de53a2acf14f3116581c"
        },
        "date": 1770653761138,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 517.7548134326935,
            "unit": "ns",
            "range": "± 1.5718059486091773"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3203.582458768572,
            "unit": "ns",
            "range": "± 7.270625678329906"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 550.9802251543317,
            "unit": "ns",
            "range": "± 1.2264500855159615"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 623.2529140313467,
            "unit": "ns",
            "range": "± 1.9961717181832341"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 627.4318762461345,
            "unit": "ns",
            "range": "± 1.8352140054827384"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 636.7496279080709,
            "unit": "ns",
            "range": "± 1.3448546333376792"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 631.4656303405761,
            "unit": "ns",
            "range": "± 3.9814705692287933"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 200.70811823209127,
            "unit": "ns",
            "range": "± 1.258957849512462"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 756.3007809321085,
            "unit": "ns",
            "range": "± 6.855808907669435"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 392.2105573972066,
            "unit": "ns",
            "range": "± 3.3901908579747655"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 669.6461236817496,
            "unit": "ns",
            "range": "± 3.2848661768287486"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5432.115082804362,
            "unit": "ns",
            "range": "± 39.33374775573103"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 38317.424369303386,
            "unit": "ns",
            "range": "± 500.5810438786456"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 652.403936958313,
            "unit": "ns",
            "range": "± 3.8596800731053102"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4873.327176230295,
            "unit": "ns",
            "range": "± 27.560146768588858"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2386.4108883993968,
            "unit": "ns",
            "range": "± 10.168686344817765"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 39.141318376859026,
            "unit": "ns",
            "range": "± 0.4376467513460427"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 52.958803967634836,
            "unit": "ns",
            "range": "± 0.41125511918564084"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 511.2983523686727,
            "unit": "ns",
            "range": "± 4.846464088534575"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2405.8304146357946,
            "unit": "ns",
            "range": "± 14.428105398957886"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4092.3990320478165,
            "unit": "ns",
            "range": "± 16.76154170065562"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 100.94458099511954,
            "unit": "ns",
            "range": "± 0.6497496555023143"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 273.31986104525055,
            "unit": "ns",
            "range": "± 2.8378080440561644"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 636.4436078824496,
            "unit": "ns",
            "range": "± 13.892268621793345"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7389.587288920085,
            "unit": "ns",
            "range": "± 130.81876714438678"
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
          "id": "d80f18a5247b9accc25f7e41375c3a857c924b64",
          "message": "perf: Reduce GC allocations in DOM diffing (#58)\n\n* perf: Defer OTel CDN loading to after first paint\n\nThis optimization improves First Paint performance by:\n\n1. Installing the lightweight OTel shim synchronously (no network dependency)\n2. Deferring CDN-based OTel SDK loading to requestIdleCallback/setTimeout\n3. Never blocking the critical path (dotnet.create() -> runMain())\n\nThe shim provides full tracing functionality during startup, and the\nCDN upgrade happens transparently in the background after first paint.\n\nKey changes:\n- Extract initLocalOtelShim() as a named synchronous function\n- Extract upgradeToFullOtel() as the async CDN loading function\n- Add scheduleDeferredOtelUpgrade() to run after app initialization\n- Remove the blocking async IIFE that ran at module load\n\nPerformance impact:\n- Before: ~4800ms First Paint (OTel CDN loading blocked startup)\n- After: ~100ms First Paint (OTel loads in background)\n\nFixes #3 in Performance Optimization Plan\n\n* fix: Unwrap memo nodes in MoveChild patch generation\n\nWhen generating MoveChild patches for keyed diffing, the code was comparing\nnode types without first unwrapping memo nodes. This caused incorrect type\ncomparisons when memoized elements were involved in reordering operations.\n\nAdded UnwrapMemoNode() calls before type checking to ensure we compare the\nactual underlying Element types, not the memo wrapper types.\n\n* ci: Increase benchmark threshold to 15% for CI variance\n\nCI runner benchmarks show up to 20% variance in confidence intervals due to:\n- GC timing differences between runs\n- Shared infrastructure resource contention\n- Complex benchmarks (larger allocations) showing more variance than simple ones\n\nIncreased threshold from 110% to 115% to reduce false positives while still\ncatching genuine regressions. Local benchmarks confirmed variance patterns:\n- CreateButtonWithHandler: ±20.30% CI\n- CreateInputWithMultipleHandlers: ±19.42% CI\n\n* perf: Defer OTel CDN loading to after first paint\n\nMoved OpenTelemetry SDK loading from blocking script execution to\nrequestIdleCallback (with setTimeout fallback). This ensures:\n- First paint is not blocked by CDN latency\n- OTel loads during browser idle time after initial render\n- Graceful degradation if CDN is slow or unavailable\n\nThe shim ensures all tracing calls work immediately, with real\nimplementation hydrated asynchronously after first paint.\n\n* fix: Address OTel review comments for PR #57\n\nReview fixes from copilot-pull-request-reviewer:\n\n1. Early return if isOtelDisabled in initLocalOtelShim() to respect\n   global disable switches and avoid unnecessary shim overhead\n\n2. Expanded fetch ignore condition to cover:\n   - OTLP proxy endpoint (/otlp/v1/traces)\n   - Common collector endpoints (/v1/traces)\n   - Custom configured exporter URL\n   - Blazor framework downloads (/_framework/)\n\n3. Restore original fetch before registering full OTel instrumentations\n   to prevent double-patching and context propagation issues\n\n4. Fix setVerbosity cache invalidation - both shim and full OTel now\n   call resetVerbosityCache() so runtime verbosity changes take effect\n\n5. Fix header guard that always evaluated to true (i && i.headers)\n\n* Cache Playwright browsers in CI workflow\n\nAdd caching for Playwright browsers to improve CI performance.\n\n* perf: Reduce GC allocations in DOM diffing\n\n- Pool PatchData lists using ConcurrentQueue to avoid allocations in ApplyBatch\n- Replace ComputeLIS array allocations with ArrayPool<int>.Shared rentals\n- Replace HashSet<int> for LIS membership with ArrayPool<bool>.Shared rental\n\nBenchmark impact (js-framework-benchmark):\n- Clear (09_clear1k): 173.2ms → 159.6ms (8% improvement)\n- Clear GC time: 18.1% → 12.2% (33% reduction)\n- Swap GC time: 10.4% → 9.4% (10% reduction)\n\nAlso documents the dotnet format multi-targeting issue in memory.instructions.md",
          "timestamp": "2026-02-09T19:05:57+01:00",
          "tree_id": "6c7300a4852fa48540f6fc2faad9db7d8b447316",
          "url": "https://github.com/Picea/Abies/commit/d80f18a5247b9accc25f7e41375c3a857c924b64"
        },
        "date": 1770660940473,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 523.8527802687424,
            "unit": "ns",
            "range": "± 1.5030957817200232"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3169.956983566284,
            "unit": "ns",
            "range": "± 8.250587224544427"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 559.9870149612427,
            "unit": "ns",
            "range": "± 1.894179508281768"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 623.3429936000279,
            "unit": "ns",
            "range": "± 1.2350439700894407"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 611.9344929967608,
            "unit": "ns",
            "range": "± 2.2610401817071653"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 620.4319965998332,
            "unit": "ns",
            "range": "± 2.8799777191344367"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 650.1872434616089,
            "unit": "ns",
            "range": "± 1.994419168200901"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 195.5497942765554,
            "unit": "ns",
            "range": "± 0.8812827134591488"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 712.7647151265826,
            "unit": "ns",
            "range": "± 1.9236195706564871"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 366.835517678942,
            "unit": "ns",
            "range": "± 0.987870419588967"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 650.2898490905761,
            "unit": "ns",
            "range": "± 2.33529625530386"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5302.748942057292,
            "unit": "ns",
            "range": "± 15.318610604926233"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 36775.64470999582,
            "unit": "ns",
            "range": "± 210.88858202674766"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 624.6411854426066,
            "unit": "ns",
            "range": "± 3.9898722445489585"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4797.488340650286,
            "unit": "ns",
            "range": "± 12.165921901240308"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2371.1542167663574,
            "unit": "ns",
            "range": "± 7.585285505097804"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 37.8883880575498,
            "unit": "ns",
            "range": "± 0.3826140329364604"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 50.70730579296748,
            "unit": "ns",
            "range": "± 0.8727071789794933"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 493.8568702697754,
            "unit": "ns",
            "range": "± 5.741473315446465"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2420.239361572266,
            "unit": "ns",
            "range": "± 38.40334347791619"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4038.839292086088,
            "unit": "ns",
            "range": "± 25.279375393502455"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 98.25867835283279,
            "unit": "ns",
            "range": "± 1.5228837837020186"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 266.19114386407955,
            "unit": "ns",
            "range": "± 5.795994019887518"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 658.7931706110636,
            "unit": "ns",
            "range": "± 13.911472959902177"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7488.506660079956,
            "unit": "ns",
            "range": "± 163.35229365151315"
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
          "id": "4f00cf43304095503b41f1503b9aa03bec87aafc",
          "message": "perf: add ClearChildren optimization for bulk child removal (#61)\n\nWhen clearing all children from an element, generate a single ClearChildren\npatch instead of N individual RemoveChild patches. This uses the native\nparent.replaceChildren() API which is much faster than N remove() calls.\n\nBenchmark improvement (09_clear1k):\n- Before: 159.6ms\n- After: 91.2ms\n- Improvement: 1.75x faster (43% reduction)\n\nChanges:\n- Add ClearChildren patch type in Operations.cs\n- Add ClearChildren JSImport in Interop.cs\n- Add clearChildren function and batch handler in abies.js\n- Add handler cleanup for ClearChildren in ApplyBatch\n- Add optimization in diff membership change path\n- Add 4 unit tests for ClearChildren behavior",
          "timestamp": "2026-02-09T21:49:17+01:00",
          "tree_id": "9f9e69e57d471a3f43030a804504452036d96691",
          "url": "https://github.com/Picea/Abies/commit/4f00cf43304095503b41f1503b9aa03bec87aafc"
        },
        "date": 1770670727131,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 534.7400022234235,
            "unit": "ns",
            "range": "± 6.477297014233705"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3120.2478921072825,
            "unit": "ns",
            "range": "± 8.489924565505273"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 542.6287122453962,
            "unit": "ns",
            "range": "± 1.1708308504798761"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 654.7171774546306,
            "unit": "ns",
            "range": "± 2.1137489890245647"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 646.4038919448852,
            "unit": "ns",
            "range": "± 1.8518834898171432"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 634.6542090688433,
            "unit": "ns",
            "range": "± 1.651291178657078"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 646.8302736282349,
            "unit": "ns",
            "range": "± 1.4158237124884332"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 196.78496721812658,
            "unit": "ns",
            "range": "± 1.1735002487769286"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 734.3048697880337,
            "unit": "ns",
            "range": "± 2.352657495985117"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 369.7344273839678,
            "unit": "ns",
            "range": "± 4.546846618469802"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 656.3818593706403,
            "unit": "ns",
            "range": "± 6.335047017197688"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5252.362808227539,
            "unit": "ns",
            "range": "± 64.24953551572683"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 37493.681901041666,
            "unit": "ns",
            "range": "± 592.0715869251194"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 628.5419785635812,
            "unit": "ns",
            "range": "± 4.758583419761101"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4850.661198570615,
            "unit": "ns",
            "range": "± 77.38788421558957"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2304.2463259015763,
            "unit": "ns",
            "range": "± 8.134240641809573"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 38.66291344165802,
            "unit": "ns",
            "range": "± 0.5306220844225487"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 50.17789982808264,
            "unit": "ns",
            "range": "± 1.1580734721654966"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 479.6019512176514,
            "unit": "ns",
            "range": "± 1.7269027480648649"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2533.7805287679034,
            "unit": "ns",
            "range": "± 16.613278073465672"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 3876.7030203683034,
            "unit": "ns",
            "range": "± 31.145236997318065"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 100.25019593238831,
            "unit": "ns",
            "range": "± 1.647116136979134"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 255.31088948249817,
            "unit": "ns",
            "range": "± 3.3487864838157977"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 665.1870448900306,
            "unit": "ns",
            "range": "± 16.628714298198517"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7417.468812052409,
            "unit": "ns",
            "range": "± 88.26298060335702"
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
          "id": "ef2e12424016ff472bdbb512e120fb824f76a05e",
          "message": "perf: Optimize GetKey with fast paths and add early exit for empty children (#64)\n\n* perf: Optimize GetKey with fast paths and add early exit for empty children\n\nTwo micro-optimizations to reduce overhead in common cases:\n\n1. Early exit for both empty children arrays\n   - Avoids ArrayPool rent/return overhead when both old and new\n     children arrays are empty (common for leaf elements like buttons)\n\n2. Optimized GetKey with fast paths for common node types\n   - Check for Element first (vast majority of nodes) to avoid\n     interface dispatch overhead for IMemoNode/ILazyMemoNode\n   - Inline the Element key extraction into a separate method\n   - Use indexed loop instead of foreach for attribute scanning\n   - Add AggressiveInlining hints for hot path\n\nThese optimizations reduce per-node overhead in the diffing algorithm,\nparticularly benefiting large DOM trees with many leaf elements.\n\n* docs: fix XML doc to match actual key precedence\n\nAddress Copilot review comment: The XML doc said 'Element Id is the primary key,\nwith data-key/key attribute as fallback' but the code checks data-key/key first.\n\nUpdated to: 'data-key/key attribute is an explicit override; element Id is the default key'\nwhich accurately reflects the implementation.",
          "timestamp": "2026-02-10T11:06:14+01:00",
          "tree_id": "7f6830fb45f6bff37472ab424db9b1424bd20d5a",
          "url": "https://github.com/Picea/Abies/commit/ef2e12424016ff472bdbb512e120fb824f76a05e"
        },
        "date": 1770718550351,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 523.1513860702514,
            "unit": "ns",
            "range": "± 3.3188154999939345"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 3203.223025258382,
            "unit": "ns",
            "range": "± 6.794653100369437"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 557.3622877257211,
            "unit": "ns",
            "range": "± 1.9663630637010376"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 599.9191995348249,
            "unit": "ns",
            "range": "± 3.4725663448213613"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 629.3502912521362,
            "unit": "ns",
            "range": "± 2.8600188228247347"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 637.822909228007,
            "unit": "ns",
            "range": "± 3.0896578835616713"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 622.4009223937989,
            "unit": "ns",
            "range": "± 5.088928439048045"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 191.41019080479938,
            "unit": "ns",
            "range": "± 1.260435510995651"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 713.6502451896667,
            "unit": "ns",
            "range": "± 1.9194906480871352"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 364.7502975097069,
            "unit": "ns",
            "range": "± 0.8522103897079462"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 648.1760769623977,
            "unit": "ns",
            "range": "± 1.7233696844514654"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5226.014277866909,
            "unit": "ns",
            "range": "± 56.520603986928805"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 36775.49718017578,
            "unit": "ns",
            "range": "± 675.747211238687"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 666.2143898010254,
            "unit": "ns",
            "range": "± 5.647684807205959"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4732.156685965402,
            "unit": "ns",
            "range": "± 12.595312788528748"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2278.1222937447683,
            "unit": "ns",
            "range": "± 3.107568239683125"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 37.04493663311005,
            "unit": "ns",
            "range": "± 0.12835177940627737"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 48.94621070367949,
            "unit": "ns",
            "range": "± 0.24479765415096996"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 470.4788101832072,
            "unit": "ns",
            "range": "± 1.7525073721038755"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2281.6448768615724,
            "unit": "ns",
            "range": "± 14.21441917047626"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 3911.79095026652,
            "unit": "ns",
            "range": "± 30.137517390042625"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 95.83535969257355,
            "unit": "ns",
            "range": "± 0.2734911102156058"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 258.106845442454,
            "unit": "ns",
            "range": "± 1.0513178278357367"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 625.787608464559,
            "unit": "ns",
            "range": "± 3.5049571464871283"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7212.292665608724,
            "unit": "ns",
            "range": "± 42.85227946601623"
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
          "id": "d1884589dfcec00a66fb1c79d013704e89a24b47",
          "message": "perf: Add small-count fast path for child diffing (#63)\n\n* perf: Add small-count fast path for child diffing\n\nFor child counts below 8, use O(n²) linear scan with stackalloc\ninstead of building dictionaries. This eliminates dictionary\nallocation overhead for common cases where most elements have\nfew children.\n\nPerformance improvements (BenchmarkDotNet):\n- SmallDomDiff: 134.6 ns vs 160.2 ns (16% faster)\n- MediumDomDiff: 965.0 ns vs 1,088 ns (11% faster)\n- LargeDomDiff: 147.2 ns vs 228.9 ns (36% faster, 79% less memory)\n\njs-framework-benchmark swap1k: 115.1 ms median vs 121.6 ms (5% faster)\n\nKey changes:\n- Add SmallChildCountThreshold constant (8 elements)\n- Add DiffChildrenSmall() method with stackalloc for matching arrays\n- Add ComputeLISIntoSmall() using stackalloc instead of ArrayPool\n\nThe threshold of 8 was chosen based on profiling showing that\ndictionary allocation + hashing overhead exceeds O(n²) scan cost\nfor small n.\n\n* fix: add ClearChildren optimization to small-count fast path\n\nThe DiffChildrenSmall method was missing the ClearChildren optimization\nthat exists in DiffChildrenCore. This caused the test\nClearAllChildren_ShouldUseSingleClearChildrenPatch to fail when\nclearing small child lists (< 8 children).\n\nAdded early check for oldLength > 0 && newLength == 0 to emit\na single ClearChildren patch instead of N individual RemoveChild patches.\n\n* fix: clear stackalloc bool span before use\n\nAddress Copilot review comment: stackalloc bool[n] creates an uninitialized\nspan. ComputeLISIntoSmall only sets true for LIS positions and assumes the\nrest are false. Without clearing, this could yield nondeterministic behavior.\n\nAdded inLIS.Clear() to ensure deterministic results.",
          "timestamp": "2026-02-10T11:27:25+01:00",
          "tree_id": "522511d26e12d248d1c7a54ebbc5b044d9533163",
          "url": "https://github.com/Picea/Abies/commit/d1884589dfcec00a66fb1c79d013704e89a24b47"
        },
        "date": 1770719865826,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 405.65125237978424,
            "unit": "ns",
            "range": "± 0.6038035359312841"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2520.4886313847132,
            "unit": "ns",
            "range": "± 6.676890527216153"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 392.10936678372894,
            "unit": "ns",
            "range": "± 0.6459487636606577"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 668.331634594844,
            "unit": "ns",
            "range": "± 1.6956919168927505"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 487.71019554138184,
            "unit": "ns",
            "range": "± 1.9369902814116196"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 471.2479518254598,
            "unit": "ns",
            "range": "± 1.1776077139013357"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 480.50602201315075,
            "unit": "ns",
            "range": "± 0.8660203453939047"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 190.97805554072062,
            "unit": "ns",
            "range": "± 0.9659801770034049"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 768.0305275235858,
            "unit": "ns",
            "range": "± 5.271388468040137"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 384.5393385546548,
            "unit": "ns",
            "range": "± 2.1725569489914536"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 703.9545783315386,
            "unit": "ns",
            "range": "± 3.5641230410671154"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5550.812739780971,
            "unit": "ns",
            "range": "± 36.97340039690638"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 40149.640075683594,
            "unit": "ns",
            "range": "± 614.881182611713"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 677.4081957499186,
            "unit": "ns",
            "range": "± 5.368917745457903"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 5063.371039170485,
            "unit": "ns",
            "range": "± 44.21059212721133"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2399.492923482259,
            "unit": "ns",
            "range": "± 34.48176306546586"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 41.03117462992668,
            "unit": "ns",
            "range": "± 1.772819302047218"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 53.889156703438076,
            "unit": "ns",
            "range": "± 0.40009765273353304"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 503.12394189834595,
            "unit": "ns",
            "range": "± 4.7797660440670775"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2455.483626774379,
            "unit": "ns",
            "range": "± 16.260118150884015"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4207.526254781087,
            "unit": "ns",
            "range": "± 39.75320968978271"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 105.8505478978157,
            "unit": "ns",
            "range": "± 1.2936412037409275"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 299.0679543358939,
            "unit": "ns",
            "range": "± 3.9643690045596824"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 687.6394901911418,
            "unit": "ns",
            "range": "± 10.932913638302571"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7992.471223880083,
            "unit": "ns",
            "range": "± 279.54898256631895"
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
          "id": "ff524c970d3dee79bce5525a689cc1834ea43419",
          "message": "fix: address Copilot review comments for ArrayPool clearing (#60)\n\n- Clear inLIS array after renting from ArrayPool to avoid stale data\n- Clear List<PatchData> before returning to pool to release references\n- Remove redundant Clear() calls (clear on rent vs on return)\n- Fix stray markdown fence in memory.instructions.md",
          "timestamp": "2026-02-10T11:49:27+01:00",
          "tree_id": "c307b1f7e3a0dfda4b71d3c76ae1364ef4d468a2",
          "url": "https://github.com/Picea/Abies/commit/ff524c970d3dee79bce5525a689cc1834ea43419"
        },
        "date": 1770721170060,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 380.8551319562472,
            "unit": "ns",
            "range": "± 1.5059891109371648"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2575.009257976825,
            "unit": "ns",
            "range": "± 3.3636020728412546"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 379.6904497464498,
            "unit": "ns",
            "range": "± 2.228307731784953"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 630.0829729667076,
            "unit": "ns",
            "range": "± 1.7962941822735172"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 466.87957587608923,
            "unit": "ns",
            "range": "± 2.3846922913314694"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 466.7862602551778,
            "unit": "ns",
            "range": "± 1.0813190645845552"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 467.4774089959952,
            "unit": "ns",
            "range": "± 1.928131911586127"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 197.27962439060212,
            "unit": "ns",
            "range": "± 1.6695298812766879"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 778.534655204186,
            "unit": "ns",
            "range": "± 6.459051695687756"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 386.86363458633423,
            "unit": "ns",
            "range": "± 2.949905992878476"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 705.9313118798392,
            "unit": "ns",
            "range": "± 2.5486619945898785"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5687.679157002767,
            "unit": "ns",
            "range": "± 21.943879935919494"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 38680.043204171314,
            "unit": "ns",
            "range": "± 450.458049578025"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 653.9587871006557,
            "unit": "ns",
            "range": "± 9.959739904238262"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 5088.568842206682,
            "unit": "ns",
            "range": "± 32.60261564919392"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2479.961382627487,
            "unit": "ns",
            "range": "± 47.24450919192442"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 38.16249174674352,
            "unit": "ns",
            "range": "± 0.7686492087572077"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 51.628154661506414,
            "unit": "ns",
            "range": "± 1.0709756831577415"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 543.6211392084757,
            "unit": "ns",
            "range": "± 8.531972391212395"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2435.2593101501466,
            "unit": "ns",
            "range": "± 44.419610756388636"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4318.9550043741865,
            "unit": "ns",
            "range": "± 46.17158590943417"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 117.15408070087433,
            "unit": "ns",
            "range": "± 1.8020463531714384"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 279.2969244321187,
            "unit": "ns",
            "range": "± 4.397489645361146"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 668.2784360779656,
            "unit": "ns",
            "range": "± 14.148113978410809"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7637.963277180989,
            "unit": "ns",
            "range": "± 128.66224831296356"
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
          "id": "2579e202090c2ff709dc0ce48a02d0432a9cd5e4",
          "message": "perf: Add clear fast path optimization for child diffing (#66)\n\n- Add O(1) early exit when clearing all children (newLength == 0)\n- Add O(n) early exit when adding all children (oldLength == 0)\n- Skip expensive dictionary building for these common cases\n- Remove dead code (redundant ClearChildren check)\n\nBenchmark results:\n- Clear (09_clear1k): 90.4ms → 85.1ms (5.9% faster)\n- Still 1.84x slower than Blazor (vs 1.96x before)",
          "timestamp": "2026-02-10T14:44:29+01:00",
          "tree_id": "1f38858a23a240b7f5ba0eefd24d3f39d0fa6542",
          "url": "https://github.com/Picea/Abies/commit/2579e202090c2ff709dc0ce48a02d0432a9cd5e4"
        },
        "date": 1770731631552,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 388.9248483181,
            "unit": "ns",
            "range": "± 1.4921235854196375"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2440.227075576782,
            "unit": "ns",
            "range": "± 3.617598791769199"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 400.30329857553755,
            "unit": "ns",
            "range": "± 0.7548147352960665"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 632.9571135203043,
            "unit": "ns",
            "range": "± 1.7462534297414058"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 481.175254208701,
            "unit": "ns",
            "range": "± 1.4314236266954659"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 461.6015610013689,
            "unit": "ns",
            "range": "± 0.8270346177857429"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 479.4535911423819,
            "unit": "ns",
            "range": "± 1.6627284225842975"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 187.80944842497507,
            "unit": "ns",
            "range": "± 1.185653283846708"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 722.3592273167202,
            "unit": "ns",
            "range": "± 2.5122940274258116"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 365.84102691014607,
            "unit": "ns",
            "range": "± 1.1224412983881211"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 653.3166077477591,
            "unit": "ns",
            "range": "± 1.060832200861246"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5333.707481384277,
            "unit": "ns",
            "range": "± 34.93757724139959"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 37981.76620076497,
            "unit": "ns",
            "range": "± 411.34461851261005"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 638.2049382073538,
            "unit": "ns",
            "range": "± 2.1323010185772207"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4867.960867745535,
            "unit": "ns",
            "range": "± 20.52610384841733"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2443.0651746114095,
            "unit": "ns",
            "range": "± 11.668893595369646"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 37.96906517102168,
            "unit": "ns",
            "range": "± 0.11003030890371329"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 51.39485574563344,
            "unit": "ns",
            "range": "± 0.22951025863431052"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 488.44682250704085,
            "unit": "ns",
            "range": "± 4.7263901215664985"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2338.9750193277996,
            "unit": "ns",
            "range": "± 16.3822433646296"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4170.739211491176,
            "unit": "ns",
            "range": "± 24.549053257582703"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 97.4970791041851,
            "unit": "ns",
            "range": "± 0.26975278885384996"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 265.39961569125836,
            "unit": "ns",
            "range": "± 1.3218104572830474"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 659.3436312357585,
            "unit": "ns",
            "range": "± 4.934417839393367"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7271.253041948591,
            "unit": "ns",
            "range": "± 20.894728582521317"
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
          "id": "f4673a65179ba8b978c92dd878838567c46b4134",
          "message": "perf: Remove thread-safety overhead for single-threaded WASM (#67)\n\n* docs: Document Direct DOM Commands investigation (rejected)\n\n- JSON-based createElement approach is 17% slower than HTML strings\n- Protobuf would still be ~10-15% slower due to decode + recursive createElement\n- Blazor's advantage is shared memory, not just binary format\n- HTML strings via innerHTML is the correct approach for Abies\n- The ~4.8% parseHtmlFragment overhead is acceptable\n\n* perf: remove thread-safety overhead for single-threaded WASM\n\nWASM is inherently single-threaded, so thread-safe constructs add\nunnecessary overhead. This commit removes that overhead:\n\n- Replace ConcurrentQueue<T> with Stack<T> for all object pools (7 pools)\n- Replace ConcurrentDictionary<K,V> with Dictionary<K,V> for handler registries (3 registries)\n- Replace Interlocked.Increment with simple ++ for command ID and memo counters\n\nBenchmark results (js-framework-benchmark):\n- 01_run1k: 104.1ms (-0.9% total)\n- 05_swap1k: 118.9ms (within variance)\n- 09_clear1k: 90.4ms (-0.1% total)\n\nThe improvements are marginal (~1%) because ARM64 atomics are fast and\nthese aren't the hot paths, but the changes are correct - we shouldn't\npay for thread-safety we don't need.\n\nFiles changed:\n- Abies/DOM/Operations.cs: Stack pools, simple memo counter increments\n- Abies/Html/Events.cs: Simple command ID increment\n- Abies/Runtime.cs: Dictionary handler registries\n- Abies/Types.cs: Removed unused System.Collections.Concurrent using\n\n* fix: restore Types.cs corrupted by dotnet format multi-targeting bug\n\nThe dotnet format command introduced merge conflict markers due to the\nmulti-targeting nature of the solution (known bug documented in memory.instructions.md).\n\nThis restores Types.cs to its main branch state and only removes the\nunused System.Collections.Concurrent using directive.\n\n* fix: remove unnecessary accessibility modifiers and simplify ValueTuple to Unit\n\n* fix: correct whitespace formatting in Types.cs",
          "timestamp": "2026-02-10T20:02:11+01:00",
          "tree_id": "5777b11a1a99adbd079e18134c41cf4924a176c1",
          "url": "https://github.com/Picea/Abies/commit/f4673a65179ba8b978c92dd878838567c46b4134"
        },
        "date": 1770750745974,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 378.886647605896,
            "unit": "ns",
            "range": "± 2.363164807169772"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2306.0500319344656,
            "unit": "ns",
            "range": "± 3.624320848800715"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 368.70712324551175,
            "unit": "ns",
            "range": "± 3.7141467386608995"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 615.5609972476959,
            "unit": "ns",
            "range": "± 0.9322144168257848"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 445.53265498234674,
            "unit": "ns",
            "range": "± 0.48190807095305227"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 461.89203196305493,
            "unit": "ns",
            "range": "± 1.3738846587537341"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 452.92708924838473,
            "unit": "ns",
            "range": "± 1.7347623569977504"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 193.9945660273234,
            "unit": "ns",
            "range": "± 1.0215985634313212"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 735.0701280434927,
            "unit": "ns",
            "range": "± 1.889095110549171"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 371.8353106180827,
            "unit": "ns",
            "range": "± 6.415971755850423"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 657.1963874181112,
            "unit": "ns",
            "range": "± 9.291864276283972"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5372.539663696289,
            "unit": "ns",
            "range": "± 12.354579679299746"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 39076.024794145065,
            "unit": "ns",
            "range": "± 924.8239056250692"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 627.2488705090115,
            "unit": "ns",
            "range": "± 2.7143297887524387"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4798.3036636352535,
            "unit": "ns",
            "range": "± 28.147456339781414"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2286.6367601247935,
            "unit": "ns",
            "range": "± 5.598014572309573"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 36.87695483977978,
            "unit": "ns",
            "range": "± 0.21056005452689874"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 48.928385361035666,
            "unit": "ns",
            "range": "± 0.21280703604154805"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 481.8470859527588,
            "unit": "ns",
            "range": "± 5.147037326241375"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2285.719703420003,
            "unit": "ns",
            "range": "± 28.610076579474303"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 3808.5037466195913,
            "unit": "ns",
            "range": "± 29.093303450599038"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 99.59977983236313,
            "unit": "ns",
            "range": "± 0.3780427057956639"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 264.46104838053384,
            "unit": "ns",
            "range": "± 1.6535126857003837"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 640.0075857764796,
            "unit": "ns",
            "range": "± 14.12730488145388"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7275.438844408308,
            "unit": "ns",
            "range": "± 47.36783901897714"
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
          "id": "683ccc926a6daf94c690cf0a072d8401e6cd151d",
          "message": "perf: Implement binary batching protocol for DOM updates (#68)\n\n* perf: implement binary batching protocol for DOM updates\n\nBREAKING CHANGE: JSON batching has been removed in favor of binary batching.\n\n## Summary\nImplements a Blazor-inspired binary batching protocol that eliminates JSON\nserialization overhead for DOM patch operations, achieving ~17% performance\nimprovement on create benchmarks.\n\n## Changes\n\n### Binary Protocol Implementation\n- Add RenderBatchWriter.cs with LEB128 string encoding and string table deduplication\n- Use JSType.MemoryView with Span<byte> for zero-copy WASM memory transfer\n- JavaScript binary reader using DataView API\n\n### Handler Registration Bug Fix\n- Fixed critical bug where ApplyBatch wasn't registering handlers for AddChild/\n  ReplaceChild/AddRoot subtrees\n- Added pre-processing step to register handlers BEFORE DOM changes\n- Added post-processing step to unregister handlers AFTER DOM changes\n- This fix was essential for select and remove operations to work correctly\n\n### Code Cleanup\n- Removed ~469 lines of JSON batching code (UseBinaryBatching flag, JSON\n  serialization paths, PatchData records)\n- Binary batching is now the only pathway\n\n## Binary Format\n```\nHeader (8 bytes):\n  - PatchCount: int32 (4 bytes)\n  - StringTableOffset: int32 (4 bytes)\n\nPatch Entries (16 bytes each):\n  - Type: int32 (4 bytes) - BinaryPatchType enum value\n  - Field1-3: int32 (4 bytes each) - string table indices (-1 = null)\n\nString Table:\n  - LEB128 length prefix + UTF8 bytes per string\n  - String deduplication via Dictionary lookup\n```\n\n## Benchmark Results (Abies vs Blazor WASM)\n\n| Benchmark       | Abies   | Blazor  | Winner          |\n|-----------------|---------|---------|-----------------|\n| 01_run1k        | 88.1ms  | 87.4ms  | ≈ Even          |\n| 02_replace1k    | 114.3ms | 104.7ms | Blazor +9%      |\n| 03_update10th1k | 147.3ms | 95.6ms  | Blazor +35%     |\n| 04_select1k     | 122.8ms | 82.2ms  | Blazor +33%     |\n| 05_swap1k       | 122.5ms | 94.1ms  | Blazor +23%     |\n| 06_remove-one-1k| 66.4ms  | 46.7ms  | Blazor +30%     |\n| 07_create10k    | 773.9ms | 818.9ms | **Abies +5.5%** |\n\n## Test Results\n- Unit Tests: 105/105 passed\n- Integration Tests: 51/51 passed\n- All js-framework-benchmark plausibility checks pass\n\n* fix: Address PR review comments and CI validation issues\n\n- Remove duplicate comment in Operations.cs (line 979-980)\n- Update memory.instructions.md to reflect binary batching (not JSON)\n- Add historical note to blazor-performance-analysis.md",
          "timestamp": "2026-02-11T11:19:30+01:00",
          "tree_id": "e83f6e7bfdd2efc3c63f450f1b3d3001c68f6389",
          "url": "https://github.com/Picea/Abies/commit/683ccc926a6daf94c690cf0a072d8401e6cd151d"
        },
        "date": 1770805751355,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 370.9114415347576,
            "unit": "ns",
            "range": "± 5.638952319738342"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2440.359769439697,
            "unit": "ns",
            "range": "± 20.527625407463145"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 388.95552784601847,
            "unit": "ns",
            "range": "± 1.7054872815853597"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 634.1639556248982,
            "unit": "ns",
            "range": "± 4.195773656772967"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 439.440717792511,
            "unit": "ns",
            "range": "± 1.908943329619123"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 477.24874005998885,
            "unit": "ns",
            "range": "± 1.8224821809803358"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 475.99079029900685,
            "unit": "ns",
            "range": "± 2.2577188378674102"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 177.36458555289678,
            "unit": "ns",
            "range": "± 0.5620808693894316"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 691.7603307723999,
            "unit": "ns",
            "range": "± 4.04887031835247"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 360.5011762210301,
            "unit": "ns",
            "range": "± 1.553233389095955"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 637.1307488123576,
            "unit": "ns",
            "range": "± 3.1740247178898513"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5149.777890886579,
            "unit": "ns",
            "range": "± 18.74565828162032"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 36552.21202305385,
            "unit": "ns",
            "range": "± 180.02285197865655"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 603.691491331373,
            "unit": "ns",
            "range": "± 4.762424738669787"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4681.524595133464,
            "unit": "ns",
            "range": "± 39.70735922354069"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2357.3593401227677,
            "unit": "ns",
            "range": "± 12.510644605512555"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 36.55513842105866,
            "unit": "ns",
            "range": "± 0.4526781380514464"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 49.26238773266474,
            "unit": "ns",
            "range": "± 0.3198169354188677"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 461.5403749465942,
            "unit": "ns",
            "range": "± 7.507792870705709"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2214.919618334089,
            "unit": "ns",
            "range": "± 11.184186614170267"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 3726.611493791853,
            "unit": "ns",
            "range": "± 27.05646997558949"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 94.78401163021724,
            "unit": "ns",
            "range": "± 0.6997580155913824"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 264.968248128891,
            "unit": "ns",
            "range": "± 1.174461514528437"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 604.3268362045288,
            "unit": "ns",
            "range": "± 5.939820427725026"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7086.137218475342,
            "unit": "ns",
            "range": "± 56.809901535727576"
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
          "id": "dd0a68e23273f9cc078f5a98dcd17163e80ea76c",
          "message": "perf: Add generic MemoKeyEquals and view cache for 17% script improvement (#69)\n\n* perf: implement binary batching protocol for DOM updates\n\nBREAKING CHANGE: JSON batching has been removed in favor of binary batching.\n\n## Summary\nImplements a Blazor-inspired binary batching protocol that eliminates JSON\nserialization overhead for DOM patch operations, achieving ~17% performance\nimprovement on create benchmarks.\n\n## Changes\n\n### Binary Protocol Implementation\n- Add RenderBatchWriter.cs with LEB128 string encoding and string table deduplication\n- Use JSType.MemoryView with Span<byte> for zero-copy WASM memory transfer\n- JavaScript binary reader using DataView API\n\n### Handler Registration Bug Fix\n- Fixed critical bug where ApplyBatch wasn't registering handlers for AddChild/\n  ReplaceChild/AddRoot subtrees\n- Added pre-processing step to register handlers BEFORE DOM changes\n- Added post-processing step to unregister handlers AFTER DOM changes\n- This fix was essential for select and remove operations to work correctly\n\n### Code Cleanup\n- Removed ~469 lines of JSON batching code (UseBinaryBatching flag, JSON\n  serialization paths, PatchData records)\n- Binary batching is now the only pathway\n\n## Binary Format\n```\nHeader (8 bytes):\n  - PatchCount: int32 (4 bytes)\n  - StringTableOffset: int32 (4 bytes)\n\nPatch Entries (16 bytes each):\n  - Type: int32 (4 bytes) - BinaryPatchType enum value\n  - Field1-3: int32 (4 bytes each) - string table indices (-1 = null)\n\nString Table:\n  - LEB128 length prefix + UTF8 bytes per string\n  - String deduplication via Dictionary lookup\n```\n\n## Benchmark Results (Abies vs Blazor WASM)\n\n| Benchmark       | Abies   | Blazor  | Winner          |\n|-----------------|---------|---------|-----------------|\n| 01_run1k        | 88.1ms  | 87.4ms  | ≈ Even          |\n| 02_replace1k    | 114.3ms | 104.7ms | Blazor +9%      |\n| 03_update10th1k | 147.3ms | 95.6ms  | Blazor +35%     |\n| 04_select1k     | 122.8ms | 82.2ms  | Blazor +33%     |\n| 05_swap1k       | 122.5ms | 94.1ms  | Blazor +23%     |\n| 06_remove-one-1k| 66.4ms  | 46.7ms  | Blazor +30%     |\n| 07_create10k    | 773.9ms | 818.9ms | **Abies +5.5%** |\n\n## Test Results\n- Unit Tests: 105/105 passed\n- Integration Tests: 51/51 passed\n- All js-framework-benchmark plausibility checks pass\n\n* fix: Address PR review comments and CI validation issues\n\n- Remove duplicate comment in Operations.cs (line 979-980)\n- Update memory.instructions.md to reflect binary batching (not JSON)\n- Add historical note to blazor-performance-analysis.md\n\n* perf: Add head/tail skip optimization for keyed diffing\n\nImplement three-phase diff in DiffChildrenCore:\n1. Skip matching head (common prefix)\n2. Skip matching tail (common suffix)\n3. Only build key maps and run LIS on middle section\n\nThis optimization doesn't significantly improve the swap benchmark\n(only saves 2 of 1000 elements), but prepares the codebase for:\n- Append-only scenarios (chat, logs, feeds) - now ~O(1)\n- Prepend scenarios - fast head mismatch detection\n- Single item changes - minimal middle section to diff\n\nAlso handles edge cases:\n- Empty middle after skip (early return)\n- Add-only middle (no removals)\n- Remove-only middle (no additions)\n- Correct beforeId calculation when tail elements exist\n\n* perf: Add generic MemoKeyEquals and view cache for 17% script improvement\n\n- Add MemoKeyEquals() method to IMemoNode and ILazyMemoNode interfaces\n- Implement using EqualityComparer<TKey>.Default.Equals() to avoid boxing\n- Add ReferenceEquals bailout at top of DiffInternal\n- Add view cache (_lazyCache) to lazy<TKey>() for reference reuse\n- Update PreserveIds to use MemoKeyEquals()\n\nBenchmark results (js-framework-benchmark):\n- 01_run1k: 74.2ms → 61.4ms script (-17%)\n- 05_swap1k: 99.1ms → 96.9ms script (-2%)\n\n* perf: Add cache eviction and attribute same-order fast path\n\n- Add auto-trim to view cache when exceeding 2000 entries to prevent memory leaks\n- Add ClearViewCache() method for manual cache management\n- Add ViewCacheCount property for diagnostics\n- Add attribute same-order fast path in DiffAttributes to skip dictionary building\n  when old and new attributes have same count and names match positionally\n\nAttribute fast path avoids O(n) dictionary allocation + hash computation\nfor the common case where attributes don't change order.\n\nBenchmark results (js-framework-benchmark):\n- 01_run1k: 92.6ms vs 94.3ms baseline (-1.8%)\n- 09_clear1k: 93.9ms vs 95.8ms baseline (-2.0%)\n- 05_swap1k: 123.4ms vs 120.8ms baseline (+2.2%, within variance)\n\nHandler cache was tested but rejected due to record equality overhead -\neach render creates new message record instances, making cache lookups\nmore expensive than the cache saves.\n\n* perf: Replace List<byte> with pooled byte[] in RenderBatchWriter\n\n- Replace List<byte> _stringData with pooled byte[] _stringBuffer\n- Add EnsureStringBufferCapacity for dynamic growth\n- Write LEB128 directly to buffer instead of via List\n- Use Encoding.UTF8.GetBytes(span, destination) for zero-allocation encoding\n- Return both buffers to ArrayPool in Dispose\n\nThis eliminates per-character allocations and reduces GC pressure\nin the hot path for binary DOM patching.\n\n* fix: Use span-based DiffChildrenSmallSpan to avoid ToArray() allocation",
          "timestamp": "2026-02-11T18:18:08+01:00",
          "tree_id": "be4ac88b1b72cec989ff8e77c8103eeae7d07739",
          "url": "https://github.com/Picea/Abies/commit/dd0a68e23273f9cc078f5a98dcd17163e80ea76c"
        },
        "date": 1770830849768,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 314.1106587648392,
            "unit": "ns",
            "range": "± 0.44742268728190876"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2012.0041721050557,
            "unit": "ns",
            "range": "± 1.9605891454885578"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 406.16069991247997,
            "unit": "ns",
            "range": "± 0.5501518341206039"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 546.6417537689209,
            "unit": "ns",
            "range": "± 1.3963356158565383"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 473.5025806427002,
            "unit": "ns",
            "range": "± 0.6553962534368233"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 445.77965913500105,
            "unit": "ns",
            "range": "± 0.7022004732230933"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 469.6235051790873,
            "unit": "ns",
            "range": "± 2.0962703755436274"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 153.0317705790202,
            "unit": "ns",
            "range": "± 0.4186040726273242"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 657.113655771528,
            "unit": "ns",
            "range": "± 2.646752158689162"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 371.8803185394832,
            "unit": "ns",
            "range": "± 1.0767613579508728"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 559.9023147583008,
            "unit": "ns",
            "range": "± 2.4347124128427753"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 4345.6711283365885,
            "unit": "ns",
            "range": "± 35.831285795824556"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 38231.72126464844,
            "unit": "ns",
            "range": "± 365.33505319895886"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 655.9027573512151,
            "unit": "ns",
            "range": "± 2.1267929047678704"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4208.487962341309,
            "unit": "ns",
            "range": "± 13.387676951782646"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2087.419017246791,
            "unit": "ns",
            "range": "± 10.740565875123226"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 42.723803850015,
            "unit": "ns",
            "range": "± 0.27048385231406086"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 57.00789872407913,
            "unit": "ns",
            "range": "± 0.5884903833789757"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 567.4052378109524,
            "unit": "ns",
            "range": "± 2.2792332882744746"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2742.395272064209,
            "unit": "ns",
            "range": "± 22.875973855660547"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4565.306034342448,
            "unit": "ns",
            "range": "± 26.612857808402428"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 119.64200469425747,
            "unit": "ns",
            "range": "± 0.8135285130794361"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 318.15994860331216,
            "unit": "ns",
            "range": "± 2.3051872911315794"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 795.0300647735596,
            "unit": "ns",
            "range": "± 6.695342067882057"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 8730.391910807291,
            "unit": "ns",
            "range": "± 37.41057086536822"
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
          "id": "c4264f5eff759193c9b1d4fc617d369b5dc70816",
          "message": "docs: Add dual-layer benchmarking strategy (#70)\n\n* docs: Add dual-layer benchmarking strategy\n\nImplement the recommended benchmarking approach based on deep research\nof how Blazor, React, Vue, and other frameworks handle performance testing.\n\nKey changes:\n- Add docs/investigations/benchmarking-strategy.md with full analysis\n- Add scripts/compare-benchmark.py for baseline comparison\n- Update benchmark.yml with E2E benchmark job (manual trigger)\n- Update memory.instructions.md with strategy summary\n\nDual-Layer Strategy:\n1. PRIMARY (Source of Truth): js-framework-benchmark\n   - Measures real user-perceived latency (EventDispatch → Paint)\n   - Must validate before merging ANY performance-related PR\n\n2. SECONDARY (Development Guidance): BenchmarkDotNet micro-benchmarks\n   - Fast feedback for algorithm comparison and allocation tracking\n   - May show false positives due to missing JS interop overhead\n\nCRITICAL RULE: Never ship based on micro-benchmark improvements alone.\n\nHistorical evidence: PatchType enum optimization showed 11-20%\nmicro-benchmark improvement but caused 2-5% REGRESSION in E2E benchmarks.\n\n* feat: Add E2E benchmark trend tracking to gh-pages\n\n- Add convert-e2e-results.py to transform js-framework-benchmark\n  results to github-action-benchmark format\n- Update benchmark.yml to store E2E results in gh-pages for\n  historical trend tracking\n- Now both micro-benchmarks AND E2E benchmarks are tracked over time\n\nThis enables visualization of E2E performance trends alongside\nmicro-benchmark trends, providing the complete picture.\n\n* fix: Add human-readable descriptions to E2E benchmark names\n\nEach benchmark now has a descriptive name in gh-pages:\n- 01_run1k (create 1000 rows)\n- 05_swap1k (swap two rows)\n- 09_clear1k (clear all rows)\n\nThis makes the trend charts more readable.\n\n* feat: Add local benchmarking script and workflow documentation\n\n- Add scripts/run-benchmarks.sh for consistent local benchmark execution\n- Support --micro, --e2e, --quick, --compare, --update-baseline options\n- Document local benchmarking workflow in benchmarking-strategy.md\n- Update .gitignore to preserve baseline.json while ignoring local results\n\n* fix: Make micro-benchmarks non-blocking, E2E is quality gate\n\n- Micro-benchmarks now use continue-on-error: true (informational only)\n- E2E js-framework-benchmark remains the blocking quality gate\n- Updated header comments to clarify blocking vs non-blocking\n\n* feat: Auto-trigger E2E benchmarks for performance PRs\n\nE2E benchmarks now run automatically when:\n- PR title starts with 'perf:' or 'perf(' (Conventional Commits)\n- PR has 'performance' label\n- Push to main (baseline tracking)\n- Manual workflow_dispatch\n\nThis ensures the quality gate is enforced without manual intervention.\n\n* fix: Remove path filter blocking E2E benchmark on PRs\n\n- Remove global path filter on pull_request trigger\n- Add dedicated 'changes' job with dorny/paths-filter\n- Micro-benchmarks only run when Abies/** paths change\n- E2E benchmarks run on perf PRs regardless of paths changed\n\n* fix: Only update gh-pages on main branch builds\n\n- Remove gh-pages updates from PR builds\n- Both micro and E2E benchmarks only push to gh-pages on main\n- PRs still get benchmark results but don't pollute trend data\n\n* perf: Add caching for npm and Chrome in E2E benchmark\n\n- Cache npm dependencies (~/.npm)\n- Cache Chrome for Selenium (~/.cache/selenium)\n- Speeds up E2E benchmark runs\n\n* fix: Trigger E2E benchmark when benchmark workflow/scripts change\n\n* fix: Copy E2E results to workspace before artifact upload\n\n* fix: Use correct js-framework-benchmark repo (krausest/js-framework-benchmark)\n\n* fix: Use static cache key for npm (can't hash files outside workspace)\n\n* feat: Add js-framework-benchmark scaffold and fix E2E workflow\n\n- Add contrib/js-framework-benchmark/ with benchmark implementation\n- Update workflow to set up Abies framework in upstream benchmark repo\n- This allows running benchmarks without needing a pre-configured fork\n\n* fix: Address review comments on benchmark comparison\n\n- Use statistics.median for proper median calculation\n- Remove unused TRACKED_BENCHMARKS and PERFORMANCE_TARGETS constants\n- Exit with error code 1 when baseline is missing in CI\n- Replace flaky sleep with curl readiness check (30s timeout)\n- Add step to fetch baseline from gh-pages if not in repo\n- Update docs: JSON serialization → binary batch building\n\n* fix: Copy Global folder for Abies build in E2E benchmark\n\nThe Abies.csproj references Global/Usings.cs and Global/Suppressions.cs\nwhich need to be present in the build context.\n\n* fix(benchmark): exclude Abies sources from AbiesBenchmark compilation\n\nSDK-style projects auto-include all .cs files in subdirectories. Since\nAbies/ is a subfolder of src/, all Abies source files were being compiled\ninto both Abies.dll (via ProjectReference) AND AbiesBenchmark.dll directly.\n\nThis caused CS0121 'ambiguous call' errors because every type existed twice.\n\nFix: Add explicit <Compile Remove=\"Abies/**/*.cs\" /> to exclude Abies\nsources from AbiesBenchmark compilation - they should only be referenced\nvia the ProjectReference.\n\n* fix(benchmark): compile webdriver-ts TypeScript before running benchmarks\n\nThe npm run bench command requires dist/benchmarkRunner.js which is\ngenerated by compiling TypeScript sources with 'npm run compile'.\n\n* fix(benchmark): use correct framework path format keyed/abies\n\nThe js-framework-benchmark expects format 'keyed/frameworkname' not\n'frameworkname-keyed' as documented in the README.\n\n* fix(benchmark): add package-lock.json required by /ls endpoint\n\nThe js-framework-benchmark server's /ls endpoint requires both\npackage.json AND package-lock.json to exist in each framework directory\nbefore it will be included in framework discovery.\n\n* fix(benchmark): handle nested values format from js-framework-benchmark\n\nCPU benchmarks output format:\n{values: {total: {median, values: [...]}, script: {...}, paint: {...}}}\n\nNot the flat array format the scripts expected. Now correctly extracts\nthe 'total' timing from nested structure.\n\n* fix(benchmark): handle empty baseline file gracefully\n\nWhen gh-pages doesn't have e2e-baseline.json, git show fails silently\nand creates an empty file. The script now handles this case by\nreturning an empty baseline dict rather than crashing.\n\n* fix(benchmark): allow first run without baseline in CI\n\nThe first E2E benchmark run cannot compare against a baseline that\ndoesn't exist yet. Changed the behavior from failing to:\n- Print current results\n- Exit with code 0 (pass)\n- Indicate baseline will be created after merge to main\n\nThe baseline gets created when results are pushed to gh-pages after\nmerging to main. Subsequent PR runs will then compare against it.",
          "timestamp": "2026-02-12T10:59:18+01:00",
          "tree_id": "b00253bcdd2f100b2b5bae902f84a03468802fed",
          "url": "https://github.com/Picea/Abies/commit/c4264f5eff759193c9b1d4fc617d369b5dc70816"
        },
        "date": 1770890954575,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 329.8522448539734,
            "unit": "ns",
            "range": "± 1.333262205104402"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2110.316234588623,
            "unit": "ns",
            "range": "± 5.387443838591742"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 404.99267152377536,
            "unit": "ns",
            "range": "± 0.9393263856003368"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 555.351982943217,
            "unit": "ns",
            "range": "± 1.4288498345053295"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 398.24102707703906,
            "unit": "ns",
            "range": "± 0.3462711473071636"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 422.15215692520144,
            "unit": "ns",
            "range": "± 1.8013720167640181"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 445.1096281638512,
            "unit": "ns",
            "range": "± 1.214878264314777"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 189.81584796905517,
            "unit": "ns",
            "range": "± 1.1434787438476979"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 761.7974975449698,
            "unit": "ns",
            "range": "± 3.6326557447043775"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 375.87462322528546,
            "unit": "ns",
            "range": "± 1.8538106255121276"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 695.0231897354126,
            "unit": "ns",
            "range": "± 4.249756389281578"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 5509.366297403972,
            "unit": "ns",
            "range": "± 43.415259277932336"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 39717.76473388672,
            "unit": "ns",
            "range": "± 532.5926303948282"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 661.1698612848918,
            "unit": "ns",
            "range": "± 5.8395899010714905"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 5054.506542205811,
            "unit": "ns",
            "range": "± 36.31569980175813"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2669.4024152119955,
            "unit": "ns",
            "range": "± 21.945604394714334"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 39.436695702870686,
            "unit": "ns",
            "range": "± 0.4706086946630569"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 56.45786761045456,
            "unit": "ns",
            "range": "± 0.7521866517877702"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 524.7874598821004,
            "unit": "ns",
            "range": "± 6.190194744171592"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2470.149972098214,
            "unit": "ns",
            "range": "± 21.93760370242869"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4071.947133890788,
            "unit": "ns",
            "range": "± 46.47451044629143"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 110.20190502007803,
            "unit": "ns",
            "range": "± 1.020527938795508"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 286.67680530548097,
            "unit": "ns",
            "range": "± 4.532032891396769"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 688.0075454030718,
            "unit": "ns",
            "range": "± 6.591771382677887"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7812.222460428874,
            "unit": "ns",
            "range": "± 66.17987744915413"
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
          "id": "1bd5d86b808a8bd846df00b7b115e3e3ecdd1b94",
          "message": "fix: Remove span wrapper from text nodes for js-framework-benchmark compliance (#71)\n\n* fix: remove span wrapper from text nodes for js-framework-benchmark compliance\n\nText nodes were previously wrapped in <span id='...'> elements, causing:\n1. HTML structure mismatch in js-framework-benchmark tests\n2. Framework being incorrectly categorized as non-keyed\n\nChanges:\n- Render text content directly without span wrapper in Operations.cs\n- UpdateText struct now includes parent element reference\n- Binary patch targets parent element, finds text node via childNodes\n- JavaScript handler updated to find and update first text node child\n\nAddresses review comments from js-framework-benchmark PR #1971.\n\n* docs: Add labeling guidelines for pull requests to improve categorization",
          "timestamp": "2026-02-12T13:23:16+01:00",
          "tree_id": "69523f5be92717237d9f733c475e97a2d03891e8",
          "url": "https://github.com/Picea/Abies/commit/1bd5d86b808a8bd846df00b7b115e3e3ecdd1b94"
        },
        "date": 1770899570193,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 319.2865589582003,
            "unit": "ns",
            "range": "± 0.6717726763299886"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 2157.655335998535,
            "unit": "ns",
            "range": "± 3.2641701313824716"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 408.9456012930189,
            "unit": "ns",
            "range": "± 0.7035876608638381"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 592.1987182753427,
            "unit": "ns",
            "range": "± 1.2772093354095284"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 437.84774899482727,
            "unit": "ns",
            "range": "± 0.3452748574695851"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 460.6287005864657,
            "unit": "ns",
            "range": "± 0.6109799081817584"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 456.55247424443564,
            "unit": "ns",
            "range": "± 0.5391865821892451"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 135.0056007385254,
            "unit": "ns",
            "range": "± 0.6313917693903478"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 615.7890211105347,
            "unit": "ns",
            "range": "± 1.5169143075958829"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 359.54142786906317,
            "unit": "ns",
            "range": "± 0.6458171307996545"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 505.23966561830963,
            "unit": "ns",
            "range": "± 3.4213170929196006"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 3964.0076538085937,
            "unit": "ns",
            "range": "± 14.858828737699886"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 34256.76986897786,
            "unit": "ns",
            "range": "± 313.6305778766808"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 629.7704960959298,
            "unit": "ns",
            "range": "± 1.0835102222864645"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 3607.3615398406982,
            "unit": "ns",
            "range": "± 11.417650534023501"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2023.7469576322114,
            "unit": "ns",
            "range": "± 6.805189689834026"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 44.09053014431681,
            "unit": "ns",
            "range": "± 0.14053895279049408"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 57.69572554429372,
            "unit": "ns",
            "range": "± 1.1093964758318395"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 568.5382364136832,
            "unit": "ns",
            "range": "± 2.397636584693114"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2748.8809975215368,
            "unit": "ns",
            "range": "± 18.044581748210803"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4603.733956400553,
            "unit": "ns",
            "range": "± 31.007116703925597"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 118.51360324223836,
            "unit": "ns",
            "range": "± 0.9551387477109764"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 313.9496976852417,
            "unit": "ns",
            "range": "± 3.8676994011661763"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 766.9997859954834,
            "unit": "ns",
            "range": "± 7.325815558357059"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 8838.055752563476,
            "unit": "ns",
            "range": "± 30.798997953062905"
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
          "id": "62c8dd894f28980360dae04ff68f54eb14b08199",
          "message": "perf: add startup performance benchmarks and E2E quality gates (#40)",
          "timestamp": "2026-02-06T17:30:09+01:00",
          "tree_id": "72d4ba0bf8d8ec8d16568309be1748fda28f3c67",
          "url": "https://github.com/Picea/Abies/commit/62c8dd894f28980360dae04ff68f54eb14b08199"
        },
        "date": 1770395984473,
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
          "id": "86f123a11af62392ed0db6c94a79d9fff9e026e1",
          "message": "perf: Batch DOM operations into single JS interop call (#48)\n\n* perf: batch DOM operations into single JS interop call\n\nReplaces individual JS interop calls per DOM operation with a single\nbatched call that applies all pending operations at once. This reduces\nthe overhead of crossing the WASM/JS boundary, especially for views\nwith many elements.\n\nChanges:\n- Add Operations.cs with JSON-serializable DOM operation types\n- Add applyPatch function in abies.js for batch processing\n- Update Interop.cs to use batched patching\n- Add PatchOperationArray to AbiesJsonContext for source generation\n- Update Runtime.cs to use new batched diffing\n\nThis optimization is particularly impactful for large lists and\ncomplex views where many DOM operations occur during a single update.\n\n* fix: address Copilot review feedback\n\n- Use parseHtmlFragment for AddChild instead of insertAdjacentHTML\n- Use addEventListeners(element) to register event handlers on inserted nodes\n- Add parentNode guard in RemoveChild before calling remove()\n- Sync textarea.value in UpdateText/UpdateTextWithId like updateTextContent\n- Remove unused variable assignment in RemoveHandler case (Operations.cs)\n\n* fix: align batched attribute handling with non-batched behavior\n\nBoolean attributes in UpdateAttribute/AddAttribute now use empty string\nvalues like the non-batched updateAttribute/addAttribute functions do.\nAlso reorder conditionals to check value first, then boolean attrs.\n\n* ci: Implement same-job benchmark comparison for accurate PR validation\n\n- Add scripts/compare-benchmarks.py for same-job comparison (no runner variance)\n- Update benchmark workflow to run both main and PR benchmarks in same job\n- Same-job comparison is the pass/fail gate (thresholds: 110% throughput, 120% allocs)\n- benchmark-action still runs but with fail-on-alert=false (for gh-pages trends only)\n- Add benchmark-results/ and benchmark-baseline/ to gitignore\n\nThis eliminates false positives caused by CI runner variance between separate jobs.\n\n* chore: sync abies.js copies and cleanup whitespace",
          "timestamp": "2026-02-08T15:47:48+01:00",
          "tree_id": "f12974c3f5e30ef8d7974caf7045727908e6000a",
          "url": "https://github.com/Picea/Abies/commit/86f123a11af62392ed0db6c94a79d9fff9e026e1"
        },
        "date": 1770562616698,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 14.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 16.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 18.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
          "id": "671652a954493dcdaba4de53a2acf14f3116581c",
          "message": "perf: Defer OTel CDN loading to after first paint (#57)\n\n* perf: Defer OTel CDN loading to after first paint\n\nThis optimization improves First Paint performance by:\n\n1. Installing the lightweight OTel shim synchronously (no network dependency)\n2. Deferring CDN-based OTel SDK loading to requestIdleCallback/setTimeout\n3. Never blocking the critical path (dotnet.create() -> runMain())\n\nThe shim provides full tracing functionality during startup, and the\nCDN upgrade happens transparently in the background after first paint.\n\nKey changes:\n- Extract initLocalOtelShim() as a named synchronous function\n- Extract upgradeToFullOtel() as the async CDN loading function\n- Add scheduleDeferredOtelUpgrade() to run after app initialization\n- Remove the blocking async IIFE that ran at module load\n\nPerformance impact:\n- Before: ~4800ms First Paint (OTel CDN loading blocked startup)\n- After: ~100ms First Paint (OTel loads in background)\n\nFixes #3 in Performance Optimization Plan\n\n* fix: Unwrap memo nodes in MoveChild patch generation\n\nWhen generating MoveChild patches for keyed diffing, the code was comparing\nnode types without first unwrapping memo nodes. This caused incorrect type\ncomparisons when memoized elements were involved in reordering operations.\n\nAdded UnwrapMemoNode() calls before type checking to ensure we compare the\nactual underlying Element types, not the memo wrapper types.\n\n* ci: Increase benchmark threshold to 15% for CI variance\n\nCI runner benchmarks show up to 20% variance in confidence intervals due to:\n- GC timing differences between runs\n- Shared infrastructure resource contention\n- Complex benchmarks (larger allocations) showing more variance than simple ones\n\nIncreased threshold from 110% to 115% to reduce false positives while still\ncatching genuine regressions. Local benchmarks confirmed variance patterns:\n- CreateButtonWithHandler: ±20.30% CI\n- CreateInputWithMultipleHandlers: ±19.42% CI\n\n* perf: Defer OTel CDN loading to after first paint\n\nMoved OpenTelemetry SDK loading from blocking script execution to\nrequestIdleCallback (with setTimeout fallback). This ensures:\n- First paint is not blocked by CDN latency\n- OTel loads during browser idle time after initial render\n- Graceful degradation if CDN is slow or unavailable\n\nThe shim ensures all tracing calls work immediately, with real\nimplementation hydrated asynchronously after first paint.\n\n* fix: Address OTel review comments for PR #57\n\nReview fixes from copilot-pull-request-reviewer:\n\n1. Early return if isOtelDisabled in initLocalOtelShim() to respect\n   global disable switches and avoid unnecessary shim overhead\n\n2. Expanded fetch ignore condition to cover:\n   - OTLP proxy endpoint (/otlp/v1/traces)\n   - Common collector endpoints (/v1/traces)\n   - Custom configured exporter URL\n   - Blazor framework downloads (/_framework/)\n\n3. Restore original fetch before registering full OTel instrumentations\n   to prevent double-patching and context propagation issues\n\n4. Fix setVerbosity cache invalidation - both shim and full OTel now\n   call resetVerbosityCache() so runtime verbosity changes take effect\n\n5. Fix header guard that always evaluated to true (i && i.headers)\n\n* Cache Playwright browsers in CI workflow\n\nAdd caching for Playwright browsers to improve CI performance.",
          "timestamp": "2026-02-09T17:06:22+01:00",
          "tree_id": "e29d6aaddfb9ec0e0ab3aef0977276e7636af2c4",
          "url": "https://github.com/Picea/Abies/commit/671652a954493dcdaba4de53a2acf14f3116581c"
        },
        "date": 1770653762908,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 14.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 16.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 18.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
          "id": "d80f18a5247b9accc25f7e41375c3a857c924b64",
          "message": "perf: Reduce GC allocations in DOM diffing (#58)\n\n* perf: Defer OTel CDN loading to after first paint\n\nThis optimization improves First Paint performance by:\n\n1. Installing the lightweight OTel shim synchronously (no network dependency)\n2. Deferring CDN-based OTel SDK loading to requestIdleCallback/setTimeout\n3. Never blocking the critical path (dotnet.create() -> runMain())\n\nThe shim provides full tracing functionality during startup, and the\nCDN upgrade happens transparently in the background after first paint.\n\nKey changes:\n- Extract initLocalOtelShim() as a named synchronous function\n- Extract upgradeToFullOtel() as the async CDN loading function\n- Add scheduleDeferredOtelUpgrade() to run after app initialization\n- Remove the blocking async IIFE that ran at module load\n\nPerformance impact:\n- Before: ~4800ms First Paint (OTel CDN loading blocked startup)\n- After: ~100ms First Paint (OTel loads in background)\n\nFixes #3 in Performance Optimization Plan\n\n* fix: Unwrap memo nodes in MoveChild patch generation\n\nWhen generating MoveChild patches for keyed diffing, the code was comparing\nnode types without first unwrapping memo nodes. This caused incorrect type\ncomparisons when memoized elements were involved in reordering operations.\n\nAdded UnwrapMemoNode() calls before type checking to ensure we compare the\nactual underlying Element types, not the memo wrapper types.\n\n* ci: Increase benchmark threshold to 15% for CI variance\n\nCI runner benchmarks show up to 20% variance in confidence intervals due to:\n- GC timing differences between runs\n- Shared infrastructure resource contention\n- Complex benchmarks (larger allocations) showing more variance than simple ones\n\nIncreased threshold from 110% to 115% to reduce false positives while still\ncatching genuine regressions. Local benchmarks confirmed variance patterns:\n- CreateButtonWithHandler: ±20.30% CI\n- CreateInputWithMultipleHandlers: ±19.42% CI\n\n* perf: Defer OTel CDN loading to after first paint\n\nMoved OpenTelemetry SDK loading from blocking script execution to\nrequestIdleCallback (with setTimeout fallback). This ensures:\n- First paint is not blocked by CDN latency\n- OTel loads during browser idle time after initial render\n- Graceful degradation if CDN is slow or unavailable\n\nThe shim ensures all tracing calls work immediately, with real\nimplementation hydrated asynchronously after first paint.\n\n* fix: Address OTel review comments for PR #57\n\nReview fixes from copilot-pull-request-reviewer:\n\n1. Early return if isOtelDisabled in initLocalOtelShim() to respect\n   global disable switches and avoid unnecessary shim overhead\n\n2. Expanded fetch ignore condition to cover:\n   - OTLP proxy endpoint (/otlp/v1/traces)\n   - Common collector endpoints (/v1/traces)\n   - Custom configured exporter URL\n   - Blazor framework downloads (/_framework/)\n\n3. Restore original fetch before registering full OTel instrumentations\n   to prevent double-patching and context propagation issues\n\n4. Fix setVerbosity cache invalidation - both shim and full OTel now\n   call resetVerbosityCache() so runtime verbosity changes take effect\n\n5. Fix header guard that always evaluated to true (i && i.headers)\n\n* Cache Playwright browsers in CI workflow\n\nAdd caching for Playwright browsers to improve CI performance.\n\n* perf: Reduce GC allocations in DOM diffing\n\n- Pool PatchData lists using ConcurrentQueue to avoid allocations in ApplyBatch\n- Replace ComputeLIS array allocations with ArrayPool<int>.Shared rentals\n- Replace HashSet<int> for LIS membership with ArrayPool<bool>.Shared rental\n\nBenchmark impact (js-framework-benchmark):\n- Clear (09_clear1k): 173.2ms → 159.6ms (8% improvement)\n- Clear GC time: 18.1% → 12.2% (33% reduction)\n- Swap GC time: 10.4% → 9.4% (10% reduction)\n\nAlso documents the dotnet format multi-targeting issue in memory.instructions.md",
          "timestamp": "2026-02-09T19:05:57+01:00",
          "tree_id": "6c7300a4852fa48540f6fc2faad9db7d8b447316",
          "url": "https://github.com/Picea/Abies/commit/d80f18a5247b9accc25f7e41375c3a857c924b64"
        },
        "date": 1770660941808,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 14.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 16.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 18.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
          "id": "4f00cf43304095503b41f1503b9aa03bec87aafc",
          "message": "perf: add ClearChildren optimization for bulk child removal (#61)\n\nWhen clearing all children from an element, generate a single ClearChildren\npatch instead of N individual RemoveChild patches. This uses the native\nparent.replaceChildren() API which is much faster than N remove() calls.\n\nBenchmark improvement (09_clear1k):\n- Before: 159.6ms\n- After: 91.2ms\n- Improvement: 1.75x faster (43% reduction)\n\nChanges:\n- Add ClearChildren patch type in Operations.cs\n- Add ClearChildren JSImport in Interop.cs\n- Add clearChildren function and batch handler in abies.js\n- Add handler cleanup for ClearChildren in ApplyBatch\n- Add optimization in diff membership change path\n- Add 4 unit tests for ClearChildren behavior",
          "timestamp": "2026-02-09T21:49:17+01:00",
          "tree_id": "9f9e69e57d471a3f43030a804504452036d96691",
          "url": "https://github.com/Picea/Abies/commit/4f00cf43304095503b41f1503b9aa03bec87aafc"
        },
        "date": 1770670729540,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 14.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 16.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 18.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
            "extra": "Gen0: 200.0000, Gen1: 9.0000"
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
            "extra": "Gen0: 94.0000, Gen1: 7.0000"
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
          "id": "ef2e12424016ff472bdbb512e120fb824f76a05e",
          "message": "perf: Optimize GetKey with fast paths and add early exit for empty children (#64)\n\n* perf: Optimize GetKey with fast paths and add early exit for empty children\n\nTwo micro-optimizations to reduce overhead in common cases:\n\n1. Early exit for both empty children arrays\n   - Avoids ArrayPool rent/return overhead when both old and new\n     children arrays are empty (common for leaf elements like buttons)\n\n2. Optimized GetKey with fast paths for common node types\n   - Check for Element first (vast majority of nodes) to avoid\n     interface dispatch overhead for IMemoNode/ILazyMemoNode\n   - Inline the Element key extraction into a separate method\n   - Use indexed loop instead of foreach for attribute scanning\n   - Add AggressiveInlining hints for hot path\n\nThese optimizations reduce per-node overhead in the diffing algorithm,\nparticularly benefiting large DOM trees with many leaf elements.\n\n* docs: fix XML doc to match actual key precedence\n\nAddress Copilot review comment: The XML doc said 'Element Id is the primary key,\nwith data-key/key attribute as fallback' but the code checks data-key/key first.\n\nUpdated to: 'data-key/key attribute is an explicit override; element Id is the default key'\nwhich accurately reflects the implementation.",
          "timestamp": "2026-02-10T11:06:14+01:00",
          "tree_id": "7f6830fb45f6bff37472ab424db9b1424bd20d5a",
          "url": "https://github.com/Picea/Abies/commit/ef2e12424016ff472bdbb512e120fb824f76a05e"
        },
        "date": 1770718552741,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 14.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 16.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 18.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
            "extra": "Gen0: 207.0000, Gen1: 1.0000"
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
            "extra": "Gen0: 200.0000, Gen1: 9.0000"
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
          "id": "2579e202090c2ff709dc0ce48a02d0432a9cd5e4",
          "message": "perf: Add clear fast path optimization for child diffing (#66)\n\n- Add O(1) early exit when clearing all children (newLength == 0)\n- Add O(n) early exit when adding all children (oldLength == 0)\n- Skip expensive dictionary building for these common cases\n- Remove dead code (redundant ClearChildren check)\n\nBenchmark results:\n- Clear (09_clear1k): 90.4ms → 85.1ms (5.9% faster)\n- Still 1.84x slower than Blazor (vs 1.96x before)",
          "timestamp": "2026-02-10T14:44:29+01:00",
          "tree_id": "1f38858a23a240b7f5ba0eefd24d3f39d0fa6542",
          "url": "https://github.com/Picea/Abies/commit/2579e202090c2ff709dc0ce48a02d0432a9cd5e4"
        },
        "date": 1770731634361,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 18.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
          "id": "f4673a65179ba8b978c92dd878838567c46b4134",
          "message": "perf: Remove thread-safety overhead for single-threaded WASM (#67)\n\n* docs: Document Direct DOM Commands investigation (rejected)\n\n- JSON-based createElement approach is 17% slower than HTML strings\n- Protobuf would still be ~10-15% slower due to decode + recursive createElement\n- Blazor's advantage is shared memory, not just binary format\n- HTML strings via innerHTML is the correct approach for Abies\n- The ~4.8% parseHtmlFragment overhead is acceptable\n\n* perf: remove thread-safety overhead for single-threaded WASM\n\nWASM is inherently single-threaded, so thread-safe constructs add\nunnecessary overhead. This commit removes that overhead:\n\n- Replace ConcurrentQueue<T> with Stack<T> for all object pools (7 pools)\n- Replace ConcurrentDictionary<K,V> with Dictionary<K,V> for handler registries (3 registries)\n- Replace Interlocked.Increment with simple ++ for command ID and memo counters\n\nBenchmark results (js-framework-benchmark):\n- 01_run1k: 104.1ms (-0.9% total)\n- 05_swap1k: 118.9ms (within variance)\n- 09_clear1k: 90.4ms (-0.1% total)\n\nThe improvements are marginal (~1%) because ARM64 atomics are fast and\nthese aren't the hot paths, but the changes are correct - we shouldn't\npay for thread-safety we don't need.\n\nFiles changed:\n- Abies/DOM/Operations.cs: Stack pools, simple memo counter increments\n- Abies/Html/Events.cs: Simple command ID increment\n- Abies/Runtime.cs: Dictionary handler registries\n- Abies/Types.cs: Removed unused System.Collections.Concurrent using\n\n* fix: restore Types.cs corrupted by dotnet format multi-targeting bug\n\nThe dotnet format command introduced merge conflict markers due to the\nmulti-targeting nature of the solution (known bug documented in memory.instructions.md).\n\nThis restores Types.cs to its main branch state and only removes the\nunused System.Collections.Concurrent using directive.\n\n* fix: remove unnecessary accessibility modifiers and simplify ValueTuple to Unit\n\n* fix: correct whitespace formatting in Types.cs",
          "timestamp": "2026-02-10T20:02:11+01:00",
          "tree_id": "5777b11a1a99adbd079e18134c41cf4924a176c1",
          "url": "https://github.com/Picea/Abies/commit/f4673a65179ba8b978c92dd878838567c46b4134"
        },
        "date": 1770750747771,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 37.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
            "extra": "Gen0: 200.0000, Gen1: 9.0000"
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
          "id": "683ccc926a6daf94c690cf0a072d8401e6cd151d",
          "message": "perf: Implement binary batching protocol for DOM updates (#68)\n\n* perf: implement binary batching protocol for DOM updates\n\nBREAKING CHANGE: JSON batching has been removed in favor of binary batching.\n\n## Summary\nImplements a Blazor-inspired binary batching protocol that eliminates JSON\nserialization overhead for DOM patch operations, achieving ~17% performance\nimprovement on create benchmarks.\n\n## Changes\n\n### Binary Protocol Implementation\n- Add RenderBatchWriter.cs with LEB128 string encoding and string table deduplication\n- Use JSType.MemoryView with Span<byte> for zero-copy WASM memory transfer\n- JavaScript binary reader using DataView API\n\n### Handler Registration Bug Fix\n- Fixed critical bug where ApplyBatch wasn't registering handlers for AddChild/\n  ReplaceChild/AddRoot subtrees\n- Added pre-processing step to register handlers BEFORE DOM changes\n- Added post-processing step to unregister handlers AFTER DOM changes\n- This fix was essential for select and remove operations to work correctly\n\n### Code Cleanup\n- Removed ~469 lines of JSON batching code (UseBinaryBatching flag, JSON\n  serialization paths, PatchData records)\n- Binary batching is now the only pathway\n\n## Binary Format\n```\nHeader (8 bytes):\n  - PatchCount: int32 (4 bytes)\n  - StringTableOffset: int32 (4 bytes)\n\nPatch Entries (16 bytes each):\n  - Type: int32 (4 bytes) - BinaryPatchType enum value\n  - Field1-3: int32 (4 bytes each) - string table indices (-1 = null)\n\nString Table:\n  - LEB128 length prefix + UTF8 bytes per string\n  - String deduplication via Dictionary lookup\n```\n\n## Benchmark Results (Abies vs Blazor WASM)\n\n| Benchmark       | Abies   | Blazor  | Winner          |\n|-----------------|---------|---------|-----------------|\n| 01_run1k        | 88.1ms  | 87.4ms  | ≈ Even          |\n| 02_replace1k    | 114.3ms | 104.7ms | Blazor +9%      |\n| 03_update10th1k | 147.3ms | 95.6ms  | Blazor +35%     |\n| 04_select1k     | 122.8ms | 82.2ms  | Blazor +33%     |\n| 05_swap1k       | 122.5ms | 94.1ms  | Blazor +23%     |\n| 06_remove-one-1k| 66.4ms  | 46.7ms  | Blazor +30%     |\n| 07_create10k    | 773.9ms | 818.9ms | **Abies +5.5%** |\n\n## Test Results\n- Unit Tests: 105/105 passed\n- Integration Tests: 51/51 passed\n- All js-framework-benchmark plausibility checks pass\n\n* fix: Address PR review comments and CI validation issues\n\n- Remove duplicate comment in Operations.cs (line 979-980)\n- Update memory.instructions.md to reflect binary batching (not JSON)\n- Add historical note to blazor-performance-analysis.md",
          "timestamp": "2026-02-11T11:19:30+01:00",
          "tree_id": "e83f6e7bfdd2efc3c63f450f1b3d3001c68f6389",
          "url": "https://github.com/Picea/Abies/commit/683ccc926a6daf94c690cf0a072d8401e6cd151d"
        },
        "date": 1770805753315,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 37.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
            "extra": "Gen0: 207.0000, Gen1: 1.0000"
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
            "extra": "Gen0: 200.0000, Gen1: 9.0000"
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
          "id": "c4264f5eff759193c9b1d4fc617d369b5dc70816",
          "message": "docs: Add dual-layer benchmarking strategy (#70)\n\n* docs: Add dual-layer benchmarking strategy\n\nImplement the recommended benchmarking approach based on deep research\nof how Blazor, React, Vue, and other frameworks handle performance testing.\n\nKey changes:\n- Add docs/investigations/benchmarking-strategy.md with full analysis\n- Add scripts/compare-benchmark.py for baseline comparison\n- Update benchmark.yml with E2E benchmark job (manual trigger)\n- Update memory.instructions.md with strategy summary\n\nDual-Layer Strategy:\n1. PRIMARY (Source of Truth): js-framework-benchmark\n   - Measures real user-perceived latency (EventDispatch → Paint)\n   - Must validate before merging ANY performance-related PR\n\n2. SECONDARY (Development Guidance): BenchmarkDotNet micro-benchmarks\n   - Fast feedback for algorithm comparison and allocation tracking\n   - May show false positives due to missing JS interop overhead\n\nCRITICAL RULE: Never ship based on micro-benchmark improvements alone.\n\nHistorical evidence: PatchType enum optimization showed 11-20%\nmicro-benchmark improvement but caused 2-5% REGRESSION in E2E benchmarks.\n\n* feat: Add E2E benchmark trend tracking to gh-pages\n\n- Add convert-e2e-results.py to transform js-framework-benchmark\n  results to github-action-benchmark format\n- Update benchmark.yml to store E2E results in gh-pages for\n  historical trend tracking\n- Now both micro-benchmarks AND E2E benchmarks are tracked over time\n\nThis enables visualization of E2E performance trends alongside\nmicro-benchmark trends, providing the complete picture.\n\n* fix: Add human-readable descriptions to E2E benchmark names\n\nEach benchmark now has a descriptive name in gh-pages:\n- 01_run1k (create 1000 rows)\n- 05_swap1k (swap two rows)\n- 09_clear1k (clear all rows)\n\nThis makes the trend charts more readable.\n\n* feat: Add local benchmarking script and workflow documentation\n\n- Add scripts/run-benchmarks.sh for consistent local benchmark execution\n- Support --micro, --e2e, --quick, --compare, --update-baseline options\n- Document local benchmarking workflow in benchmarking-strategy.md\n- Update .gitignore to preserve baseline.json while ignoring local results\n\n* fix: Make micro-benchmarks non-blocking, E2E is quality gate\n\n- Micro-benchmarks now use continue-on-error: true (informational only)\n- E2E js-framework-benchmark remains the blocking quality gate\n- Updated header comments to clarify blocking vs non-blocking\n\n* feat: Auto-trigger E2E benchmarks for performance PRs\n\nE2E benchmarks now run automatically when:\n- PR title starts with 'perf:' or 'perf(' (Conventional Commits)\n- PR has 'performance' label\n- Push to main (baseline tracking)\n- Manual workflow_dispatch\n\nThis ensures the quality gate is enforced without manual intervention.\n\n* fix: Remove path filter blocking E2E benchmark on PRs\n\n- Remove global path filter on pull_request trigger\n- Add dedicated 'changes' job with dorny/paths-filter\n- Micro-benchmarks only run when Abies/** paths change\n- E2E benchmarks run on perf PRs regardless of paths changed\n\n* fix: Only update gh-pages on main branch builds\n\n- Remove gh-pages updates from PR builds\n- Both micro and E2E benchmarks only push to gh-pages on main\n- PRs still get benchmark results but don't pollute trend data\n\n* perf: Add caching for npm and Chrome in E2E benchmark\n\n- Cache npm dependencies (~/.npm)\n- Cache Chrome for Selenium (~/.cache/selenium)\n- Speeds up E2E benchmark runs\n\n* fix: Trigger E2E benchmark when benchmark workflow/scripts change\n\n* fix: Copy E2E results to workspace before artifact upload\n\n* fix: Use correct js-framework-benchmark repo (krausest/js-framework-benchmark)\n\n* fix: Use static cache key for npm (can't hash files outside workspace)\n\n* feat: Add js-framework-benchmark scaffold and fix E2E workflow\n\n- Add contrib/js-framework-benchmark/ with benchmark implementation\n- Update workflow to set up Abies framework in upstream benchmark repo\n- This allows running benchmarks without needing a pre-configured fork\n\n* fix: Address review comments on benchmark comparison\n\n- Use statistics.median for proper median calculation\n- Remove unused TRACKED_BENCHMARKS and PERFORMANCE_TARGETS constants\n- Exit with error code 1 when baseline is missing in CI\n- Replace flaky sleep with curl readiness check (30s timeout)\n- Add step to fetch baseline from gh-pages if not in repo\n- Update docs: JSON serialization → binary batch building\n\n* fix: Copy Global folder for Abies build in E2E benchmark\n\nThe Abies.csproj references Global/Usings.cs and Global/Suppressions.cs\nwhich need to be present in the build context.\n\n* fix(benchmark): exclude Abies sources from AbiesBenchmark compilation\n\nSDK-style projects auto-include all .cs files in subdirectories. Since\nAbies/ is a subfolder of src/, all Abies source files were being compiled\ninto both Abies.dll (via ProjectReference) AND AbiesBenchmark.dll directly.\n\nThis caused CS0121 'ambiguous call' errors because every type existed twice.\n\nFix: Add explicit <Compile Remove=\"Abies/**/*.cs\" /> to exclude Abies\nsources from AbiesBenchmark compilation - they should only be referenced\nvia the ProjectReference.\n\n* fix(benchmark): compile webdriver-ts TypeScript before running benchmarks\n\nThe npm run bench command requires dist/benchmarkRunner.js which is\ngenerated by compiling TypeScript sources with 'npm run compile'.\n\n* fix(benchmark): use correct framework path format keyed/abies\n\nThe js-framework-benchmark expects format 'keyed/frameworkname' not\n'frameworkname-keyed' as documented in the README.\n\n* fix(benchmark): add package-lock.json required by /ls endpoint\n\nThe js-framework-benchmark server's /ls endpoint requires both\npackage.json AND package-lock.json to exist in each framework directory\nbefore it will be included in framework discovery.\n\n* fix(benchmark): handle nested values format from js-framework-benchmark\n\nCPU benchmarks output format:\n{values: {total: {median, values: [...]}, script: {...}, paint: {...}}}\n\nNot the flat array format the scripts expected. Now correctly extracts\nthe 'total' timing from nested structure.\n\n* fix(benchmark): handle empty baseline file gracefully\n\nWhen gh-pages doesn't have e2e-baseline.json, git show fails silently\nand creates an empty file. The script now handles this case by\nreturning an empty baseline dict rather than crashing.\n\n* fix(benchmark): allow first run without baseline in CI\n\nThe first E2E benchmark run cannot compare against a baseline that\ndoesn't exist yet. Changed the behavior from failing to:\n- Print current results\n- Exit with code 0 (pass)\n- Indicate baseline will be created after merge to main\n\nThe baseline gets created when results are pushed to gh-pages after\nmerging to main. Subsequent PR runs will then compare against it.",
          "timestamp": "2026-02-12T10:59:18+01:00",
          "tree_id": "b00253bcdd2f100b2b5bae902f84a03468802fed",
          "url": "https://github.com/Picea/Abies/commit/c4264f5eff759193c9b1d4fc617d369b5dc70816"
        },
        "date": 1770890957062,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 672,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 240,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 37.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 320,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1392,
            "unit": "bytes",
            "extra": "Gen0: 87.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 776,
            "unit": "bytes",
            "extra": "Gen0: 97.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 1144,
            "unit": "bytes",
            "extra": "Gen0: 71.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 9944,
            "unit": "bytes",
            "extra": "Gen0: 77.0000"
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
            "value": 9384,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4848,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
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
          "id": "1bd5d86b808a8bd846df00b7b115e3e3ecdd1b94",
          "message": "fix: Remove span wrapper from text nodes for js-framework-benchmark compliance (#71)\n\n* fix: remove span wrapper from text nodes for js-framework-benchmark compliance\n\nText nodes were previously wrapped in <span id='...'> elements, causing:\n1. HTML structure mismatch in js-framework-benchmark tests\n2. Framework being incorrectly categorized as non-keyed\n\nChanges:\n- Render text content directly without span wrapper in Operations.cs\n- UpdateText struct now includes parent element reference\n- Binary patch targets parent element, finds text node via childNodes\n- JavaScript handler updated to find and update first text node child\n\nAddresses review comments from js-framework-benchmark PR #1971.\n\n* docs: Add labeling guidelines for pull requests to improve categorization",
          "timestamp": "2026-02-12T13:23:16+01:00",
          "tree_id": "69523f5be92717237d9f733c475e97a2d03891e8",
          "url": "https://github.com/Picea/Abies/commit/1bd5d86b808a8bd846df00b7b115e3e3ecdd1b94"
        },
        "date": 1770899572613,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 18.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 688,
            "unit": "bytes",
            "extra": "Gen0: 7.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 248,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 24.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 264,
            "unit": "bytes",
            "extra": "Gen0: 44.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1224,
            "unit": "bytes",
            "extra": "Gen0: 51.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 728,
            "unit": "bytes",
            "extra": "Gen0: 60.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 928,
            "unit": "bytes",
            "extra": "Gen0: 38.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 8160,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Rendering/RenderLargePage",
            "value": 121424,
            "unit": "bytes",
            "extra": "Gen0: 79.0000, Gen1: 15.0000"
          },
          {
            "name": "Rendering/RenderDeeplyNested",
            "value": 1168,
            "unit": "bytes",
            "extra": "Gen0: 48.0000"
          },
          {
            "name": "Rendering/RenderWideTree",
            "value": 6688,
            "unit": "bytes",
            "extra": "Gen0: 69.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4304,
            "unit": "bytes",
            "extra": "Gen0: 44.0000"
          },
          {
            "name": "Handlers/CreateSingleHandler_Message",
            "value": 120,
            "unit": "bytes",
            "extra": "Gen0: 80.0000"
          },
          {
            "name": "Handlers/CreateSingleHandler_Factory",
            "value": 208,
            "unit": "bytes",
            "extra": "Gen0: 69.0000"
          },
          {
            "name": "Handlers/Create10Handlers",
            "value": 1656,
            "unit": "bytes",
            "extra": "Gen0: 69.0000"
          },
          {
            "name": "Handlers/Create50Handlers",
            "value": 8184,
            "unit": "bytes",
            "extra": "Gen0: 85.0000, Gen1: 2.0000"
          },
          {
            "name": "Handlers/Create100Handlers",
            "value": 12824,
            "unit": "bytes",
            "extra": "Gen0: 66.0000, Gen1: 2.0000"
          },
          {
            "name": "Handlers/CreateButtonWithHandler",
            "value": 400,
            "unit": "bytes",
            "extra": "Gen0: 66.0000"
          },
          {
            "name": "Handlers/CreateInputWithMultipleHandlers",
            "value": 976,
            "unit": "bytes",
            "extra": "Gen0: 81.0000"
          },
          {
            "name": "Handlers/CreateFormWithHandlers",
            "value": 2424,
            "unit": "bytes",
            "extra": "Gen0: 101.0000"
          },
          {
            "name": "Handlers/CreateArticleListWithHandlers",
            "value": 24104,
            "unit": "bytes",
            "extra": "Gen0: 62.0000, Gen1: 4.0000"
          }
        ]
      }
    ],
    "E2E Benchmark (js-framework-benchmark)": [
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
          "id": "c4264f5eff759193c9b1d4fc617d369b5dc70816",
          "message": "docs: Add dual-layer benchmarking strategy (#70)\n\n* docs: Add dual-layer benchmarking strategy\n\nImplement the recommended benchmarking approach based on deep research\nof how Blazor, React, Vue, and other frameworks handle performance testing.\n\nKey changes:\n- Add docs/investigations/benchmarking-strategy.md with full analysis\n- Add scripts/compare-benchmark.py for baseline comparison\n- Update benchmark.yml with E2E benchmark job (manual trigger)\n- Update memory.instructions.md with strategy summary\n\nDual-Layer Strategy:\n1. PRIMARY (Source of Truth): js-framework-benchmark\n   - Measures real user-perceived latency (EventDispatch → Paint)\n   - Must validate before merging ANY performance-related PR\n\n2. SECONDARY (Development Guidance): BenchmarkDotNet micro-benchmarks\n   - Fast feedback for algorithm comparison and allocation tracking\n   - May show false positives due to missing JS interop overhead\n\nCRITICAL RULE: Never ship based on micro-benchmark improvements alone.\n\nHistorical evidence: PatchType enum optimization showed 11-20%\nmicro-benchmark improvement but caused 2-5% REGRESSION in E2E benchmarks.\n\n* feat: Add E2E benchmark trend tracking to gh-pages\n\n- Add convert-e2e-results.py to transform js-framework-benchmark\n  results to github-action-benchmark format\n- Update benchmark.yml to store E2E results in gh-pages for\n  historical trend tracking\n- Now both micro-benchmarks AND E2E benchmarks are tracked over time\n\nThis enables visualization of E2E performance trends alongside\nmicro-benchmark trends, providing the complete picture.\n\n* fix: Add human-readable descriptions to E2E benchmark names\n\nEach benchmark now has a descriptive name in gh-pages:\n- 01_run1k (create 1000 rows)\n- 05_swap1k (swap two rows)\n- 09_clear1k (clear all rows)\n\nThis makes the trend charts more readable.\n\n* feat: Add local benchmarking script and workflow documentation\n\n- Add scripts/run-benchmarks.sh for consistent local benchmark execution\n- Support --micro, --e2e, --quick, --compare, --update-baseline options\n- Document local benchmarking workflow in benchmarking-strategy.md\n- Update .gitignore to preserve baseline.json while ignoring local results\n\n* fix: Make micro-benchmarks non-blocking, E2E is quality gate\n\n- Micro-benchmarks now use continue-on-error: true (informational only)\n- E2E js-framework-benchmark remains the blocking quality gate\n- Updated header comments to clarify blocking vs non-blocking\n\n* feat: Auto-trigger E2E benchmarks for performance PRs\n\nE2E benchmarks now run automatically when:\n- PR title starts with 'perf:' or 'perf(' (Conventional Commits)\n- PR has 'performance' label\n- Push to main (baseline tracking)\n- Manual workflow_dispatch\n\nThis ensures the quality gate is enforced without manual intervention.\n\n* fix: Remove path filter blocking E2E benchmark on PRs\n\n- Remove global path filter on pull_request trigger\n- Add dedicated 'changes' job with dorny/paths-filter\n- Micro-benchmarks only run when Abies/** paths change\n- E2E benchmarks run on perf PRs regardless of paths changed\n\n* fix: Only update gh-pages on main branch builds\n\n- Remove gh-pages updates from PR builds\n- Both micro and E2E benchmarks only push to gh-pages on main\n- PRs still get benchmark results but don't pollute trend data\n\n* perf: Add caching for npm and Chrome in E2E benchmark\n\n- Cache npm dependencies (~/.npm)\n- Cache Chrome for Selenium (~/.cache/selenium)\n- Speeds up E2E benchmark runs\n\n* fix: Trigger E2E benchmark when benchmark workflow/scripts change\n\n* fix: Copy E2E results to workspace before artifact upload\n\n* fix: Use correct js-framework-benchmark repo (krausest/js-framework-benchmark)\n\n* fix: Use static cache key for npm (can't hash files outside workspace)\n\n* feat: Add js-framework-benchmark scaffold and fix E2E workflow\n\n- Add contrib/js-framework-benchmark/ with benchmark implementation\n- Update workflow to set up Abies framework in upstream benchmark repo\n- This allows running benchmarks without needing a pre-configured fork\n\n* fix: Address review comments on benchmark comparison\n\n- Use statistics.median for proper median calculation\n- Remove unused TRACKED_BENCHMARKS and PERFORMANCE_TARGETS constants\n- Exit with error code 1 when baseline is missing in CI\n- Replace flaky sleep with curl readiness check (30s timeout)\n- Add step to fetch baseline from gh-pages if not in repo\n- Update docs: JSON serialization → binary batch building\n\n* fix: Copy Global folder for Abies build in E2E benchmark\n\nThe Abies.csproj references Global/Usings.cs and Global/Suppressions.cs\nwhich need to be present in the build context.\n\n* fix(benchmark): exclude Abies sources from AbiesBenchmark compilation\n\nSDK-style projects auto-include all .cs files in subdirectories. Since\nAbies/ is a subfolder of src/, all Abies source files were being compiled\ninto both Abies.dll (via ProjectReference) AND AbiesBenchmark.dll directly.\n\nThis caused CS0121 'ambiguous call' errors because every type existed twice.\n\nFix: Add explicit <Compile Remove=\"Abies/**/*.cs\" /> to exclude Abies\nsources from AbiesBenchmark compilation - they should only be referenced\nvia the ProjectReference.\n\n* fix(benchmark): compile webdriver-ts TypeScript before running benchmarks\n\nThe npm run bench command requires dist/benchmarkRunner.js which is\ngenerated by compiling TypeScript sources with 'npm run compile'.\n\n* fix(benchmark): use correct framework path format keyed/abies\n\nThe js-framework-benchmark expects format 'keyed/frameworkname' not\n'frameworkname-keyed' as documented in the README.\n\n* fix(benchmark): add package-lock.json required by /ls endpoint\n\nThe js-framework-benchmark server's /ls endpoint requires both\npackage.json AND package-lock.json to exist in each framework directory\nbefore it will be included in framework discovery.\n\n* fix(benchmark): handle nested values format from js-framework-benchmark\n\nCPU benchmarks output format:\n{values: {total: {median, values: [...]}, script: {...}, paint: {...}}}\n\nNot the flat array format the scripts expected. Now correctly extracts\nthe 'total' timing from nested structure.\n\n* fix(benchmark): handle empty baseline file gracefully\n\nWhen gh-pages doesn't have e2e-baseline.json, git show fails silently\nand creates an empty file. The script now handles this case by\nreturning an empty baseline dict rather than crashing.\n\n* fix(benchmark): allow first run without baseline in CI\n\nThe first E2E benchmark run cannot compare against a baseline that\ndoesn't exist yet. Changed the behavior from failing to:\n- Print current results\n- Exit with code 0 (pass)\n- Indicate baseline will be created after merge to main\n\nThe baseline gets created when results are pushed to gh-pages after\nmerging to main. Subsequent PR runs will then compare against it.",
          "timestamp": "2026-02-12T10:59:18+01:00",
          "tree_id": "b00253bcdd2f100b2b5bae902f84a03468802fed",
          "url": "https://github.com/Picea/Abies/commit/c4264f5eff759193c9b1d4fc617d369b5dc70816"
        },
        "date": 1770890625765,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "01_run1k (create 1000 rows)",
            "value": 250.4,
            "unit": "ms",
            "extra": "mean: 248.5ms, samples: 15"
          },
          {
            "name": "05_swap1k (swap two rows)",
            "value": 300.3,
            "unit": "ms",
            "extra": "mean: 298.6ms, samples: 15"
          },
          {
            "name": "09_clear1k_x8",
            "value": 256,
            "unit": "ms",
            "extra": "mean: 251.1ms, samples: 15"
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
          "id": "1bd5d86b808a8bd846df00b7b115e3e3ecdd1b94",
          "message": "fix: Remove span wrapper from text nodes for js-framework-benchmark compliance (#71)\n\n* fix: remove span wrapper from text nodes for js-framework-benchmark compliance\n\nText nodes were previously wrapped in <span id='...'> elements, causing:\n1. HTML structure mismatch in js-framework-benchmark tests\n2. Framework being incorrectly categorized as non-keyed\n\nChanges:\n- Render text content directly without span wrapper in Operations.cs\n- UpdateText struct now includes parent element reference\n- Binary patch targets parent element, finds text node via childNodes\n- JavaScript handler updated to find and update first text node child\n\nAddresses review comments from js-framework-benchmark PR #1971.\n\n* docs: Add labeling guidelines for pull requests to improve categorization",
          "timestamp": "2026-02-12T13:23:16+01:00",
          "tree_id": "69523f5be92717237d9f733c475e97a2d03891e8",
          "url": "https://github.com/Picea/Abies/commit/1bd5d86b808a8bd846df00b7b115e3e3ecdd1b94"
        },
        "date": 1770899252844,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "01_run1k (create 1000 rows)",
            "value": 242,
            "unit": "ms",
            "extra": "mean: 242.5ms, samples: 15"
          },
          {
            "name": "05_swap1k (swap two rows)",
            "value": 320.6,
            "unit": "ms",
            "extra": "mean: 326.1ms, samples: 15"
          },
          {
            "name": "09_clear1k_x8",
            "value": 274.1,
            "unit": "ms",
            "extra": "mean: 279.0ms, samples: 15"
          }
        ]
      }
    ],
    "2. Micro-Benchmarks: Throughput (BenchmarkDotNet)": [
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
          "id": "1cafa25388fc2867d34addd96bd8009a42a23db2",
          "message": "ci: E2E-only benchmark pipeline with memory benchmarks (#72)\n\nChanges:\n- Remove micro-benchmarks from PR builds (only run on push to main for historical tracking)\n- Add all 9 CPU benchmarks (01-09) to E2E pipeline\n- Add 3 memory benchmarks (21, 22, 25) to E2E pipeline\n- Simplify path detection (e2e-scripts now includes Abies changes)\n- Reorder gh-pages reports: E2E first (1.), Micro-benchmarks second (2.)\n- Update strategy comments to reflect single source of truth approach\n\nRationale:\nHistorical evidence shows micro-benchmarks can be misleading - PatchType enum\noptimization showed 11-20% improvement in BenchmarkDotNet but caused 2-5%\nREGRESSION in E2E benchmarks. E2E is now the sole quality gate.",
          "timestamp": "2026-02-12T14:09:33+01:00",
          "tree_id": "a9794360291353bf46f5b922aefe4e3faea91f11",
          "url": "https://github.com/Picea/Abies/commit/1cafa25388fc2867d34addd96bd8009a42a23db2"
        },
        "date": 1770902369476,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 324.2305029460362,
            "unit": "ns",
            "range": "± 1.5565017273763067"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 1902.2679560343424,
            "unit": "ns",
            "range": "± 5.659852369140906"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 400.15972904058606,
            "unit": "ns",
            "range": "± 0.8253235991573554"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 549.2044494628906,
            "unit": "ns",
            "range": "± 1.355622373446064"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 404.8927569389343,
            "unit": "ns",
            "range": "± 1.2595928922686113"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 414.65508365631104,
            "unit": "ns",
            "range": "± 0.7286856478215703"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 429.5862560272217,
            "unit": "ns",
            "range": "± 1.036046461120684"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 165.05408754715552,
            "unit": "ns",
            "range": "± 0.6382483054184938"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 661.1275405883789,
            "unit": "ns",
            "range": "± 3.6681164630811502"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 328.69882961908974,
            "unit": "ns",
            "range": "± 1.2833065639427788"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 587.0751824012169,
            "unit": "ns",
            "range": "± 2.6069258265889683"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 4466.825613755446,
            "unit": "ns",
            "range": "± 10.14889143280772"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 31815.931187220984,
            "unit": "ns",
            "range": "± 95.18837691009978"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 592.8723488489787,
            "unit": "ns",
            "range": "± 1.6201923490139827"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 3831.6531278170073,
            "unit": "ns",
            "range": "± 18.062591760143306"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2131.8744411468506,
            "unit": "ns",
            "range": "± 9.820593326699932"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 36.120749396937235,
            "unit": "ns",
            "range": "± 0.1561974653353739"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 48.0857320745786,
            "unit": "ns",
            "range": "± 0.29268146638750137"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 477.45892810821533,
            "unit": "ns",
            "range": "± 1.7521396117201744"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2207.620461390569,
            "unit": "ns",
            "range": "± 11.5319449296664"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 3779.62325995309,
            "unit": "ns",
            "range": "± 14.002715980988087"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 93.62430170377095,
            "unit": "ns",
            "range": "± 0.7420859474545056"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 255.87938372294107,
            "unit": "ns",
            "range": "± 1.6072691903392415"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 614.7265769958497,
            "unit": "ns",
            "range": "± 4.129423885690714"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7356.6145842415945,
            "unit": "ns",
            "range": "± 47.60592958005666"
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
          "id": "c07cd3647150b94730f2c5c93ba12f3ef95d787e",
          "message": "chore: target .NET 10 LTS only (#73)\n\nRemove .NET 9 multi-targeting. Going forward:\n- Current release: .NET 10 (LTS)\n- Next release: .NET 11 + 10\n- Following: .NET 12 only (LTS)\n\nThis simplifies the build and aligns with LTS release strategy.",
          "timestamp": "2026-02-12T15:46:43+01:00",
          "tree_id": "257f670637d04bf2e693ff59801a8d0b352885f2",
          "url": "https://github.com/Picea/Abies/commit/c07cd3647150b94730f2c5c93ba12f3ef95d787e"
        },
        "date": 1770908178913,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 322.02576974460055,
            "unit": "ns",
            "range": "± 1.3222632188920433"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 1906.1594881330218,
            "unit": "ns",
            "range": "± 2.605122623184326"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 415.39547307150707,
            "unit": "ns",
            "range": "± 0.7002178250445948"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 551.4458859988621,
            "unit": "ns",
            "range": "± 1.6242072530898042"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 409.45548270298883,
            "unit": "ns",
            "range": "± 0.5138000648584605"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 430.6788289705912,
            "unit": "ns",
            "range": "± 5.271500674396581"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 422.66717421213787,
            "unit": "ns",
            "range": "± 1.4937212589961846"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 176.7758697827657,
            "unit": "ns",
            "range": "± 1.5174273772956735"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 732.6122060775757,
            "unit": "ns",
            "range": "± 5.377937308737382"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 347.10342960357667,
            "unit": "ns",
            "range": "± 2.0143474112534423"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 576.4231742450169,
            "unit": "ns",
            "range": "± 4.602505453533961"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 4717.146201578776,
            "unit": "ns",
            "range": "± 25.674066702090183"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 34255.40271402995,
            "unit": "ns",
            "range": "± 124.52422811115014"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 641.1277440616062,
            "unit": "ns",
            "range": "± 1.920604561629936"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4026.649607522147,
            "unit": "ns",
            "range": "± 22.424916415925253"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2280.149289703369,
            "unit": "ns",
            "range": "± 14.397312387944897"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 39.11695544719696,
            "unit": "ns",
            "range": "± 0.2855605934448666"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 53.638757383823396,
            "unit": "ns",
            "range": "± 0.9308206375359394"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 513.5738866669791,
            "unit": "ns",
            "range": "± 2.720751868283632"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2520.139575958252,
            "unit": "ns",
            "range": "± 11.933624274243913"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4189.152421804575,
            "unit": "ns",
            "range": "± 35.8813662640615"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 109.2665828148524,
            "unit": "ns",
            "range": "± 1.9052535733540994"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 268.8471380233765,
            "unit": "ns",
            "range": "± 3.1720476660875945"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 674.7047681172688,
            "unit": "ns",
            "range": "± 10.874800128479961"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7847.504373168946,
            "unit": "ns",
            "range": "± 45.33048210808258"
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
          "id": "5c6e5a29cbc803da582da63d644622960ea52bff",
          "message": "chore: fix code formatting throughout solution (#74)\n\nApply dotnet format to all non-core Abies project files to fix\npre-existing formatting issues. Add IDE0005 pragma suppression to\nGlobal/Usings.cs for the Unit alias (false positive - Unit IS used\nin the core library).\n\nNote: The core Abies project has known issues with dotnet format\ndue to multi-targeting. Those files are intentionally not modified\nto avoid corruption (see memory.instructions.md).",
          "timestamp": "2026-02-12T15:54:59+01:00",
          "tree_id": "4beb6ba39b626a573338a3d083406cab86071c1f",
          "url": "https://github.com/Picea/Abies/commit/5c6e5a29cbc803da582da63d644622960ea52bff"
        },
        "date": 1770908684049,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 325.4289929316594,
            "unit": "ns",
            "range": "± 2.0317643624876585"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 1921.0545790536064,
            "unit": "ns",
            "range": "± 6.9968761041806795"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 410.22561761311124,
            "unit": "ns",
            "range": "± 0.7059465771832931"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 554.5643453598022,
            "unit": "ns",
            "range": "± 0.7756083171768768"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 432.9335669004,
            "unit": "ns",
            "range": "± 0.5014144259577596"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 446.3286381403605,
            "unit": "ns",
            "range": "± 1.0041157003234609"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 426.51749300956726,
            "unit": "ns",
            "range": "± 0.9150884287706943"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 175.82557082176208,
            "unit": "ns",
            "range": "± 1.9629776483506538"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 741.8749857630048,
            "unit": "ns",
            "range": "± 1.750135198585081"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 375.2591583887736,
            "unit": "ns",
            "range": "± 1.8140451438363232"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 597.0457918167115,
            "unit": "ns",
            "range": "± 4.291222206920004"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 4730.868940080915,
            "unit": "ns",
            "range": "± 20.607740708306462"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 52995.510579427086,
            "unit": "ns",
            "range": "± 415.83641049751793"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 641.9736213684082,
            "unit": "ns",
            "range": "± 2.343315937712863"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4241.613216106708,
            "unit": "ns",
            "range": "± 14.842249204171729"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2352.4833847045898,
            "unit": "ns",
            "range": "± 10.087670599894079"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 40.81611239512761,
            "unit": "ns",
            "range": "± 0.44064294205614885"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 53.09139047219203,
            "unit": "ns",
            "range": "± 0.6439708519666363"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 529.9636285146078,
            "unit": "ns",
            "range": "± 5.9364669117619435"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2539.2811711629233,
            "unit": "ns",
            "range": "± 26.10205950417407"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 4247.642271314348,
            "unit": "ns",
            "range": "± 23.327941219268016"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 122.10767728090286,
            "unit": "ns",
            "range": "± 0.9359846892802329"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 295.02017485468014,
            "unit": "ns",
            "range": "± 6.34114239393435"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 710.6524100621541,
            "unit": "ns",
            "range": "± 11.528434031692974"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 8137.214294433594,
            "unit": "ns",
            "range": "± 68.30438132727355"
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
          "id": "0c0190a7cd3f87a6b7ec1b3122e83a8b9e723f58",
          "message": "ci: add separate E2E memory benchmark chart (#75)\n\nSplit CPU and memory metrics from js-framework-benchmark into\nseparate gh-pages charts. Memory benchmarks (21-25) now display\nwith correct MB unit instead of being mixed into the CPU chart\nwith ms.\n\nChanges:\n- Add --output-memory flag to convert-e2e-results.py\n- Add is_memory_benchmark() helper to filter by benchmark prefix\n- Add Store E2E memory benchmark trends step to benchmark.yml\n- Include e2e-benchmark-memory.json in artifact upload",
          "timestamp": "2026-02-12T16:24:46+01:00",
          "tree_id": "59007d3b8ef9bac1d182604e71aa0bc168b55118",
          "url": "https://github.com/Picea/Abies/commit/0c0190a7cd3f87a6b7ec1b3122e83a8b9e723f58"
        },
        "date": 1770910497834,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Abies.Benchmarks.Diffing/SmallDomDiff",
            "value": 314.986824075381,
            "unit": "ns",
            "range": "± 0.7050579204485795"
          },
          {
            "name": "Abies.Benchmarks.Diffing/MediumDomDiff",
            "value": 1929.9370894798865,
            "unit": "ns",
            "range": "± 3.123909246896205"
          },
          {
            "name": "Abies.Benchmarks.Diffing/LargeDomDiff",
            "value": 384.7255237783705,
            "unit": "ns",
            "range": "± 1.0892073777774691"
          },
          {
            "name": "Abies.Benchmarks.Diffing/AttributeOnlyDiff",
            "value": 542.787728037153,
            "unit": "ns",
            "range": "± 1.5656720084611437"
          },
          {
            "name": "Abies.Benchmarks.Diffing/TextOnlyDiff",
            "value": 402.98722801889693,
            "unit": "ns",
            "range": "± 0.4059425360137841"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeAdditionDiff",
            "value": 415.7629863875253,
            "unit": "ns",
            "range": "± 1.3689030369347683"
          },
          {
            "name": "Abies.Benchmarks.Diffing/NodeRemovalDiff",
            "value": 423.94101449421476,
            "unit": "ns",
            "range": "± 0.5220113663567134"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSimpleElement",
            "value": 172.881316391627,
            "unit": "ns",
            "range": "± 1.2849109529377882"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithHtmlEncoding",
            "value": 739.8345399220784,
            "unit": "ns",
            "range": "± 6.375129220443483"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWithEventHandlers",
            "value": 342.59095487594607,
            "unit": "ns",
            "range": "± 6.239379980502408"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderSmallPage",
            "value": 589.8102927525838,
            "unit": "ns",
            "range": "± 8.743732211421957"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderMediumPage",
            "value": 4817.736754862467,
            "unit": "ns",
            "range": "± 18.271551741375475"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderLargePage",
            "value": 37585.119794573104,
            "unit": "ns",
            "range": "± 155.0974762420483"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderDeeplyNested",
            "value": 653.141494001661,
            "unit": "ns",
            "range": "± 1.1983135892092411"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderWideTree",
            "value": 4094.525944049542,
            "unit": "ns",
            "range": "± 7.478693887327391"
          },
          {
            "name": "Abies.Benchmarks.Rendering/RenderComplexForm",
            "value": 2155.954665629069,
            "unit": "ns",
            "range": "± 26.882446482521868"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Message",
            "value": 41.60390878149441,
            "unit": "ns",
            "range": "± 0.061677130520690064"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateSingleHandler_Factory",
            "value": 57.29423186608723,
            "unit": "ns",
            "range": "± 0.5628668669918081"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create10Handlers",
            "value": 548.3303882598877,
            "unit": "ns",
            "range": "± 2.7230825349748193"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create50Handlers",
            "value": 2239.262265822467,
            "unit": "ns",
            "range": "± 43.47070695623321"
          },
          {
            "name": "Abies.Benchmarks.Handlers/Create100Handlers",
            "value": 3758.683194732666,
            "unit": "ns",
            "range": "± 73.84970236860438"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateButtonWithHandler",
            "value": 98.37693814436595,
            "unit": "ns",
            "range": "± 1.1956161549199118"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateInputWithMultipleHandlers",
            "value": 256.5078787167867,
            "unit": "ns",
            "range": "± 2.7499055884437396"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateFormWithHandlers",
            "value": 612.3179830551147,
            "unit": "ns",
            "range": "± 1.5592015562694734"
          },
          {
            "name": "Abies.Benchmarks.Handlers/CreateArticleListWithHandlers",
            "value": 7247.008454386393,
            "unit": "ns",
            "range": "± 14.677356015011377"
          }
        ]
      }
    ],
    "2. Micro-Benchmarks: Allocations (BenchmarkDotNet)": [
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
          "id": "1cafa25388fc2867d34addd96bd8009a42a23db2",
          "message": "ci: E2E-only benchmark pipeline with memory benchmarks (#72)\n\nChanges:\n- Remove micro-benchmarks from PR builds (only run on push to main for historical tracking)\n- Add all 9 CPU benchmarks (01-09) to E2E pipeline\n- Add 3 memory benchmarks (21, 22, 25) to E2E pipeline\n- Simplify path detection (e2e-scripts now includes Abies changes)\n- Reorder gh-pages reports: E2E first (1.), Micro-benchmarks second (2.)\n- Update strategy comments to reflect single source of truth approach\n\nRationale:\nHistorical evidence shows micro-benchmarks can be misleading - PatchType enum\noptimization showed 11-20% improvement in BenchmarkDotNet but caused 2-5%\nREGRESSION in E2E benchmarks. E2E is now the sole quality gate.",
          "timestamp": "2026-02-12T14:09:33+01:00",
          "tree_id": "a9794360291353bf46f5b922aefe4e3faea91f11",
          "url": "https://github.com/Picea/Abies/commit/1cafa25388fc2867d34addd96bd8009a42a23db2"
        },
        "date": 1770902370533,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 688,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 248,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 37.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 264,
            "unit": "bytes",
            "extra": "Gen0: 66.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1224,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 728,
            "unit": "bytes",
            "extra": "Gen0: 91.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 928,
            "unit": "bytes",
            "extra": "Gen0: 58.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 8160,
            "unit": "bytes",
            "extra": "Gen0: 63.0000"
          },
          {
            "name": "Rendering/RenderLargePage",
            "value": 121424,
            "unit": "bytes",
            "extra": "Gen0: 118.0000, Gen1: 23.0000"
          },
          {
            "name": "Rendering/RenderDeeplyNested",
            "value": 1168,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderWideTree",
            "value": 6688,
            "unit": "bytes",
            "extra": "Gen0: 52.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4304,
            "unit": "bytes",
            "extra": "Gen0: 67.0000"
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
            "extra": "Gen0: 207.0000, Gen1: 1.0000"
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
            "extra": "Gen0: 200.0000, Gen1: 9.0000"
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
          "id": "c07cd3647150b94730f2c5c93ba12f3ef95d787e",
          "message": "chore: target .NET 10 LTS only (#73)\n\nRemove .NET 9 multi-targeting. Going forward:\n- Current release: .NET 10 (LTS)\n- Next release: .NET 11 + 10\n- Following: .NET 12 only (LTS)\n\nThis simplifies the build and aligns with LTS release strategy.",
          "timestamp": "2026-02-12T15:46:43+01:00",
          "tree_id": "257f670637d04bf2e693ff59801a8d0b352885f2",
          "url": "https://github.com/Picea/Abies/commit/c07cd3647150b94730f2c5c93ba12f3ef95d787e"
        },
        "date": 1770908180477,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 688,
            "unit": "bytes",
            "extra": "Gen0: 21.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 248,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 37.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 264,
            "unit": "bytes",
            "extra": "Gen0: 66.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1224,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 728,
            "unit": "bytes",
            "extra": "Gen0: 91.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 928,
            "unit": "bytes",
            "extra": "Gen0: 58.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 8160,
            "unit": "bytes",
            "extra": "Gen0: 63.0000"
          },
          {
            "name": "Rendering/RenderLargePage",
            "value": 121424,
            "unit": "bytes",
            "extra": "Gen0: 118.0000, Gen1: 23.0000"
          },
          {
            "name": "Rendering/RenderDeeplyNested",
            "value": 1168,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderWideTree",
            "value": 6688,
            "unit": "bytes",
            "extra": "Gen0: 52.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4304,
            "unit": "bytes",
            "extra": "Gen0: 67.0000"
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
            "extra": "Gen0: 94.0000, Gen1: 7.0000"
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
          "id": "5c6e5a29cbc803da582da63d644622960ea52bff",
          "message": "chore: fix code formatting throughout solution (#74)\n\nApply dotnet format to all non-core Abies project files to fix\npre-existing formatting issues. Add IDE0005 pragma suppression to\nGlobal/Usings.cs for the Unit alias (false positive - Unit IS used\nin the core library).\n\nNote: The core Abies project has known issues with dotnet format\ndue to multi-targeting. Those files are intentionally not modified\nto avoid corruption (see memory.instructions.md).",
          "timestamp": "2026-02-12T15:54:59+01:00",
          "tree_id": "4beb6ba39b626a573338a3d083406cab86071c1f",
          "url": "https://github.com/Picea/Abies/commit/5c6e5a29cbc803da582da63d644622960ea52bff"
        },
        "date": 1770908686067,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 688,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 248,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 37.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 264,
            "unit": "bytes",
            "extra": "Gen0: 66.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1224,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 728,
            "unit": "bytes",
            "extra": "Gen0: 91.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 928,
            "unit": "bytes",
            "extra": "Gen0: 58.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 8160,
            "unit": "bytes",
            "extra": "Gen0: 63.0000"
          },
          {
            "name": "Rendering/RenderLargePage",
            "value": 121424,
            "unit": "bytes",
            "extra": "Gen0: 118.0000, Gen1: 23.0000"
          },
          {
            "name": "Rendering/RenderDeeplyNested",
            "value": 1168,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderWideTree",
            "value": 6688,
            "unit": "bytes",
            "extra": "Gen0: 52.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4304,
            "unit": "bytes",
            "extra": "Gen0: 67.0000"
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
            "value": 393,
            "unit": "bytes",
            "extra": "Gen0: 98.0000"
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
            "extra": "Gen0: 94.0000, Gen1: 7.0000"
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
          "id": "0c0190a7cd3f87a6b7ec1b3122e83a8b9e723f58",
          "message": "ci: add separate E2E memory benchmark chart (#75)\n\nSplit CPU and memory metrics from js-framework-benchmark into\nseparate gh-pages charts. Memory benchmarks (21-25) now display\nwith correct MB unit instead of being mixed into the CPU chart\nwith ms.\n\nChanges:\n- Add --output-memory flag to convert-e2e-results.py\n- Add is_memory_benchmark() helper to filter by benchmark prefix\n- Add Store E2E memory benchmark trends step to benchmark.yml\n- Include e2e-benchmark-memory.json in artifact upload",
          "timestamp": "2026-02-12T16:24:46+01:00",
          "tree_id": "59007d3b8ef9bac1d182604e71aa0bc168b55118",
          "url": "https://github.com/Picea/Abies/commit/0c0190a7cd3f87a6b7ec1b3122e83a8b9e723f58"
        },
        "date": 1770910499753,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Diffing/SmallDomDiff",
            "value": 224,
            "unit": "bytes",
            "extra": "Gen0: 28.0000"
          },
          {
            "name": "Diffing/MediumDomDiff",
            "value": 688,
            "unit": "bytes",
            "extra": "Gen0: 10.0000"
          },
          {
            "name": "Diffing/LargeDomDiff",
            "value": 256,
            "unit": "bytes",
            "extra": "Gen0: 32.0000"
          },
          {
            "name": "Diffing/AttributeOnlyDiff",
            "value": 248,
            "unit": "bytes",
            "extra": "Gen0: 15.0000"
          },
          {
            "name": "Diffing/TextOnlyDiff",
            "value": 296,
            "unit": "bytes",
            "extra": "Gen0: 37.0000"
          },
          {
            "name": "Diffing/NodeAdditionDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Diffing/NodeRemovalDiff",
            "value": 336,
            "unit": "bytes",
            "extra": "Gen0: 42.0000"
          },
          {
            "name": "Rendering/RenderSimpleElement",
            "value": 264,
            "unit": "bytes",
            "extra": "Gen0: 66.0000"
          },
          {
            "name": "Rendering/RenderWithHtmlEncoding",
            "value": 1224,
            "unit": "bytes",
            "extra": "Gen0: 76.0000"
          },
          {
            "name": "Rendering/RenderWithEventHandlers",
            "value": 728,
            "unit": "bytes",
            "extra": "Gen0: 91.0000"
          },
          {
            "name": "Rendering/RenderSmallPage",
            "value": 928,
            "unit": "bytes",
            "extra": "Gen0: 58.0000"
          },
          {
            "name": "Rendering/RenderMediumPage",
            "value": 8160,
            "unit": "bytes",
            "extra": "Gen0: 63.0000"
          },
          {
            "name": "Rendering/RenderLargePage",
            "value": 121424,
            "unit": "bytes",
            "extra": "Gen0: 118.0000, Gen1: 23.0000"
          },
          {
            "name": "Rendering/RenderDeeplyNested",
            "value": 1168,
            "unit": "bytes",
            "extra": "Gen0: 73.0000"
          },
          {
            "name": "Rendering/RenderWideTree",
            "value": 6688,
            "unit": "bytes",
            "extra": "Gen0: 52.0000"
          },
          {
            "name": "Rendering/RenderComplexForm",
            "value": 4304,
            "unit": "bytes",
            "extra": "Gen0: 67.0000"
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
            "extra": "Gen0: 200.0000, Gen1: 9.0000"
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
      }
    ],
    "1. E2E Benchmark (js-framework-benchmark)": [
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
          "id": "1cafa25388fc2867d34addd96bd8009a42a23db2",
          "message": "ci: E2E-only benchmark pipeline with memory benchmarks (#72)\n\nChanges:\n- Remove micro-benchmarks from PR builds (only run on push to main for historical tracking)\n- Add all 9 CPU benchmarks (01-09) to E2E pipeline\n- Add 3 memory benchmarks (21, 22, 25) to E2E pipeline\n- Simplify path detection (e2e-scripts now includes Abies changes)\n- Reorder gh-pages reports: E2E first (1.), Micro-benchmarks second (2.)\n- Update strategy comments to reflect single source of truth approach\n\nRationale:\nHistorical evidence shows micro-benchmarks can be misleading - PatchType enum\noptimization showed 11-20% improvement in BenchmarkDotNet but caused 2-5%\nREGRESSION in E2E benchmarks. E2E is now the sole quality gate.",
          "timestamp": "2026-02-12T14:09:33+01:00",
          "tree_id": "a9794360291353bf46f5b922aefe4e3faea91f11",
          "url": "https://github.com/Picea/Abies/commit/1cafa25388fc2867d34addd96bd8009a42a23db2"
        },
        "date": 1770902410472,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "01_run1k (create 1000 rows)",
            "value": 241.2,
            "unit": "ms",
            "extra": "mean: 243.9ms, samples: 15"
          },
          {
            "name": "02_replace1k (replace all 1000 rows)",
            "value": 287.9,
            "unit": "ms",
            "extra": "mean: 286.8ms, samples: 15"
          },
          {
            "name": "03_update10th1k_x16",
            "value": 334,
            "unit": "ms",
            "extra": "mean: 335.9ms, samples: 15"
          },
          {
            "name": "04_select1k (select row)",
            "value": 305,
            "unit": "ms",
            "extra": "mean: 302.3ms, samples: 25"
          },
          {
            "name": "05_swap1k (swap two rows)",
            "value": 311.1,
            "unit": "ms",
            "extra": "mean: 307.9ms, samples: 15"
          },
          {
            "name": "06_remove-one-1k (remove one row)",
            "value": 166.2,
            "unit": "ms",
            "extra": "mean: 166.7ms, samples: 15"
          },
          {
            "name": "07_create10k (create 10,000 rows)",
            "value": 2030.6,
            "unit": "ms",
            "extra": "mean: 2030.2ms, samples: 15"
          },
          {
            "name": "08_create1k-after1k_x2 (append 1000 rows)",
            "value": 290.2,
            "unit": "ms",
            "extra": "mean: 287.3ms, samples: 15"
          },
          {
            "name": "09_clear1k_x8",
            "value": 259.3,
            "unit": "ms",
            "extra": "mean: 260.4ms, samples: 15"
          },
          {
            "name": "21_ready-memory (ready memory)",
            "value": 34.47743797302246,
            "unit": "ms",
            "extra": "mean: 34.5ms, samples: 1"
          },
          {
            "name": "22_run-memory (run memory)",
            "value": 37.96933937072754,
            "unit": "ms",
            "extra": "mean: 38.0ms, samples: 1"
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
          "id": "c07cd3647150b94730f2c5c93ba12f3ef95d787e",
          "message": "chore: target .NET 10 LTS only (#73)\n\nRemove .NET 9 multi-targeting. Going forward:\n- Current release: .NET 10 (LTS)\n- Next release: .NET 11 + 10\n- Following: .NET 12 only (LTS)\n\nThis simplifies the build and aligns with LTS release strategy.",
          "timestamp": "2026-02-12T15:46:43+01:00",
          "tree_id": "257f670637d04bf2e693ff59801a8d0b352885f2",
          "url": "https://github.com/Picea/Abies/commit/c07cd3647150b94730f2c5c93ba12f3ef95d787e"
        },
        "date": 1770908247978,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "01_run1k (create 1000 rows)",
            "value": 251.1,
            "unit": "ms",
            "extra": "mean: 252.2ms, samples: 15"
          },
          {
            "name": "02_replace1k (replace all 1000 rows)",
            "value": 302.4,
            "unit": "ms",
            "extra": "mean: 300.5ms, samples: 15"
          },
          {
            "name": "03_update10th1k_x16",
            "value": 350.1,
            "unit": "ms",
            "extra": "mean: 352.1ms, samples: 15"
          },
          {
            "name": "04_select1k (select row)",
            "value": 314.8,
            "unit": "ms",
            "extra": "mean: 312.3ms, samples: 25"
          },
          {
            "name": "05_swap1k (swap two rows)",
            "value": 331.2,
            "unit": "ms",
            "extra": "mean: 330.9ms, samples: 15"
          },
          {
            "name": "06_remove-one-1k (remove one row)",
            "value": 176.5,
            "unit": "ms",
            "extra": "mean: 174.6ms, samples: 15"
          },
          {
            "name": "07_create10k (create 10,000 rows)",
            "value": 2037.3,
            "unit": "ms",
            "extra": "mean: 2036.9ms, samples: 15"
          },
          {
            "name": "08_create1k-after1k_x2 (append 1000 rows)",
            "value": 298.8,
            "unit": "ms",
            "extra": "mean: 299.2ms, samples: 15"
          },
          {
            "name": "09_clear1k_x8",
            "value": 269.3,
            "unit": "ms",
            "extra": "mean: 267.1ms, samples: 15"
          },
          {
            "name": "21_ready-memory (ready memory)",
            "value": 34.4551944732666,
            "unit": "ms",
            "extra": "mean: 34.5ms, samples: 1"
          },
          {
            "name": "22_run-memory (run memory)",
            "value": 37.92978763580322,
            "unit": "ms",
            "extra": "mean: 37.9ms, samples: 1"
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
          "id": "5c6e5a29cbc803da582da63d644622960ea52bff",
          "message": "chore: fix code formatting throughout solution (#74)\n\nApply dotnet format to all non-core Abies project files to fix\npre-existing formatting issues. Add IDE0005 pragma suppression to\nGlobal/Usings.cs for the Unit alias (false positive - Unit IS used\nin the core library).\n\nNote: The core Abies project has known issues with dotnet format\ndue to multi-targeting. Those files are intentionally not modified\nto avoid corruption (see memory.instructions.md).",
          "timestamp": "2026-02-12T15:54:59+01:00",
          "tree_id": "4beb6ba39b626a573338a3d083406cab86071c1f",
          "url": "https://github.com/Picea/Abies/commit/5c6e5a29cbc803da582da63d644622960ea52bff"
        },
        "date": 1770908805114,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "01_run1k (create 1000 rows)",
            "value": 252,
            "unit": "ms",
            "extra": "mean: 257.2ms, samples: 15"
          },
          {
            "name": "02_replace1k (replace all 1000 rows)",
            "value": 307.3,
            "unit": "ms",
            "extra": "mean: 308.3ms, samples: 15"
          },
          {
            "name": "03_update10th1k_x16",
            "value": 359.8,
            "unit": "ms",
            "extra": "mean: 362.4ms, samples: 15"
          },
          {
            "name": "04_select1k (select row)",
            "value": 316.2,
            "unit": "ms",
            "extra": "mean: 316.1ms, samples: 25"
          },
          {
            "name": "05_swap1k (swap two rows)",
            "value": 321.1,
            "unit": "ms",
            "extra": "mean: 323.4ms, samples: 15"
          },
          {
            "name": "06_remove-one-1k (remove one row)",
            "value": 182.3,
            "unit": "ms",
            "extra": "mean: 182.9ms, samples: 15"
          },
          {
            "name": "07_create10k (create 10,000 rows)",
            "value": 2128.1,
            "unit": "ms",
            "extra": "mean: 2135.6ms, samples: 15"
          },
          {
            "name": "08_create1k-after1k_x2 (append 1000 rows)",
            "value": 310.3,
            "unit": "ms",
            "extra": "mean: 308.2ms, samples: 15"
          },
          {
            "name": "09_clear1k_x8",
            "value": 273.7,
            "unit": "ms",
            "extra": "mean: 273.5ms, samples: 15"
          },
          {
            "name": "21_ready-memory (ready memory)",
            "value": 34.44077491760254,
            "unit": "ms",
            "extra": "mean: 34.4ms, samples: 1"
          },
          {
            "name": "22_run-memory (run memory)",
            "value": 37.91446495056152,
            "unit": "ms",
            "extra": "mean: 37.9ms, samples: 1"
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
          "id": "0c0190a7cd3f87a6b7ec1b3122e83a8b9e723f58",
          "message": "ci: add separate E2E memory benchmark chart (#75)\n\nSplit CPU and memory metrics from js-framework-benchmark into\nseparate gh-pages charts. Memory benchmarks (21-25) now display\nwith correct MB unit instead of being mixed into the CPU chart\nwith ms.\n\nChanges:\n- Add --output-memory flag to convert-e2e-results.py\n- Add is_memory_benchmark() helper to filter by benchmark prefix\n- Add Store E2E memory benchmark trends step to benchmark.yml\n- Include e2e-benchmark-memory.json in artifact upload",
          "timestamp": "2026-02-12T16:24:46+01:00",
          "tree_id": "59007d3b8ef9bac1d182604e71aa0bc168b55118",
          "url": "https://github.com/Picea/Abies/commit/0c0190a7cd3f87a6b7ec1b3122e83a8b9e723f58"
        },
        "date": 1770910548865,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "01_run1k (create 1000 rows)",
            "value": 252.9,
            "unit": "ms",
            "extra": "mean: 255.4ms, samples: 15"
          },
          {
            "name": "02_replace1k (replace all 1000 rows)",
            "value": 297.7,
            "unit": "ms",
            "extra": "mean: 299.1ms, samples: 15"
          },
          {
            "name": "03_update10th1k_x16",
            "value": 356.8,
            "unit": "ms",
            "extra": "mean: 362.7ms, samples: 15"
          },
          {
            "name": "04_select1k (select row)",
            "value": 321.6,
            "unit": "ms",
            "extra": "mean: 323.5ms, samples: 25"
          },
          {
            "name": "05_swap1k (swap two rows)",
            "value": 334.1,
            "unit": "ms",
            "extra": "mean: 330.5ms, samples: 15"
          },
          {
            "name": "06_remove-one-1k (remove one row)",
            "value": 180,
            "unit": "ms",
            "extra": "mean: 179.5ms, samples: 15"
          },
          {
            "name": "07_create10k (create 10,000 rows)",
            "value": 2144.3,
            "unit": "ms",
            "extra": "mean: 2140.4ms, samples: 15"
          },
          {
            "name": "08_create1k-after1k_x2 (append 1000 rows)",
            "value": 301.2,
            "unit": "ms",
            "extra": "mean: 300.0ms, samples: 15"
          },
          {
            "name": "09_clear1k_x8",
            "value": 272.5,
            "unit": "ms",
            "extra": "mean: 273.2ms, samples: 15"
          }
        ]
      }
    ]
  }
}