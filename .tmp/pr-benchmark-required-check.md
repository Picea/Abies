## 📝 Description

### What
Add a required benchmark status check that fails when js-framework-benchmark detects a performance regression over the configured threshold.

### Why
The project needs enforceable performance protection in CI so regressions are blocked before merging.

### How
- Renamed the benchmark job to provide the required check name: `Benchmark (js-framework-benchmark)`.
- Updated regression comparison flow to capture the compare step result and fail the job explicitly when regressions exceed 5%.
- Updated contribution docs to document the required benchmark check and local validation flow.

## 🔗 Related Issues

None

## ✅ Type of Change

- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [x] 📚 Documentation update
- [ ] 🎨 Code style update (formatting, renaming)
- [ ] ♻️ Refactoring (no functional changes)
- [x] ⚡ Performance improvement
- [ ] ✅ Test update
- [x] 🔧 Build/CI configuration change

## 🧪 Testing

### Test Coverage

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] E2E tests added/updated
- [x] Manual testing performed

### Testing Details

- Verified workflow file now defines job `benchmark-check` with display name `Benchmark (js-framework-benchmark)`.
- Verified explicit fail path in `Report benchmark status` step triggers when regression compare step fails.
- Verified `CONTRIBUTING.md` now documents benchmark as a required status check and includes local benchmark guidance.

## 📸 Screenshots/Videos

N/A

## ✨ Changes Made

- Renamed benchmark workflow job from `e2e-benchmark` to `benchmark-check`.
- Added explicit regression outcome handling (`id: regression-check` + status reporting step with `exit 1`).
- Updated required checks and benchmark policy docs in `CONTRIBUTING.md`.

## 🔍 Code Review Checklist

- [x] Code follows the project's style guidelines
- [x] Self-review of code performed
- [x] Comments added for complex/non-obvious code
- [x] Documentation updated (if needed)
- [x] No new warnings generated
- [x] Tests added/updated and passing
- [x] All commits follow [Conventional Commits](https://www.conventionalcommits.org/) format
- [x] Branch is up-to-date with main
- [x] No merge conflicts

## 🚀 Deployment Notes

None

## 📋 Additional Context

This change is intentionally scoped to CI enforcement and contributor documentation only; no runtime behavior was changed.
