# Pre-Commit Verification Checklist

**MANDATORY**: Follow this checklist before every commit to ensure code quality.

## Quick Reference

```bash
# 1. Run unit tests
dotnet test

# 2. Run integration tests
cd tests
.\run-integration-tests.ps1

# 3. Build with strict warnings
dotnet build --warnaserror

# 4. Only then commit
git add .
git commit -m "..."
git push
```

## Detailed Checklist

### ✅ Unit Tests

```bash
dotnet test
```

**Must pass**: All unit tests in `tests/Dottie.*.Tests/`
**Must show**: No failed tests, no skipped tests (unless documented)
**Fix if**: Tests fail → fix code, re-run tests until all pass

### ✅ Integration Tests

```bash
cd tests
.\run-integration-tests.ps1
```

**Must pass**: All scenario tests (basic-symlinks, conflict-handling, link-command)
**Must show**: "✓ All integration tests passed!"
**Fix if**: Tests fail → check Docker/scenario files, re-run until all pass

### ✅ Build & Warnings

```bash
dotnet build --warnaserror
```

**Must succeed**: Build completes without errors or warnings
**Fix if**: Analyzer warnings appear → address violations per `.specify/memory/rules.md`

### ✅ Coverage (Optional check)

```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Target**: ≥90% line coverage, ≥80% branch coverage

## CI/CD Enforcement

- **Main branch** has branch protection rules
- **All tests must pass** before PR can be merged
- **GitHub Actions** automatically runs these checks on every push
- If tests fail in CI, review the logs and fix locally

## When You Can't Run Integration Tests

If Docker isn't available locally:

1. Push to your feature branch (will run in CI)
2. Open a PR
3. GitHub Actions will run all tests
4. Fix any failures and re-push
5. Tests will re-run automatically

**Note**: This should be rare — Docker is pre-installed on most development machines.

## Contributing with Copilot

When using Copilot to make changes:

1. Ask Copilot to implement the change with tests
2. Run the tests locally (`dotnet test`)
3. Run integration tests (`.\tests\run-integration-tests.ps1`)
4. Fix any failures
5. Commit only after all tests pass

**Copilot Prompt Example**:
```
I need to add a new feature that does X.
Please implement it with:
1. Unit tests FIRST (TDD approach)
2. Implementation code
3. Integration test scenario if needed

Requirements:
- Must pass all existing tests
- Must have ≥90% line coverage
- Must follow the constitution in .specify/memory/constitution.md
```

## Troubleshooting

**Q: Tests pass locally but fail in CI?**
- Check Docker image differences (use `docker --version`)
- Re-run tests to ensure consistency
- Review CI logs at https://github.com/ryandanthony/dottie/actions

**Q: Integration tests take too long?**
- This is expected (~1-2 minutes for Docker build + tests)
- Consider running only unit tests during active development
- Run full integration tests before pushing

**Q: Can't install Docker?**
- Contact the team — Docker is required for integration tests
- You can still run unit tests: `dotnet test`
- CI/CD will validate integration tests for PRs
