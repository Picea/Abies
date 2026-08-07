; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ABIES006 | Picea.Abies.Html | Warning | Repeated interactive controls should use explicit stable handler ids
ABIES007 | Picea.Abies.Html | Warning | Repeated helper invocations that emit interactive controls should use explicit stable handler ids
ABIES008 | Picea.Abies.Native | Error | NativeElementAnalyzer, native tag collides with an HTML void element name
ABIES009 | Picea.Abies.Native | Error | NativeElementAnalyzer, Text or RawHtml node inside a native element tree
