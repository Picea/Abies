using Xunit;

// The Conduit UI ApiClient is static and configurable; these near‑E2E tests
// override global state. Disable parallelization to avoid cross-test interference.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
