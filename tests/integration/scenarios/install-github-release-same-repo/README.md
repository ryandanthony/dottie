# Integration Test: Multiple Binaries from Same GitHub Repo

## Purpose

Tests that multiple GitHub release entries from the **same repository** but with different binaries are all preserved during profile inheritance merging and installation.

This validates the fix for a bug where `ProfileMerger.MergeByKey` used only `Repo` as the dictionary key, causing the second entry to overwrite the first when two items shared the same repository.

## Configuration

The `dottie.yml` file specifies two entries from `ahmetb/kubectx`:
- **kubectx**: Kubernetes context switcher
- **kubens**: Kubernetes namespace switcher

A child profile (`work`) extends `default` to verify both entries survive profile inheritance.

## Validation

The `validate.sh` script:
1. Validates the configuration loads without errors
2. Runs `dottie install --dry-run` on the inherited profile and verifies both binaries appear in output
3. Runs the actual install and verifies both binaries are downloaded to `~/bin/`
4. Confirms both binaries are executable
