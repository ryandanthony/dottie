# Private APT Repository Installation Integration Test

## Purpose

Tests the `dottie install` command with private APT repository configuration.

## Configuration

The `dottie.yml` file specifies a private APT repository with:
- Repository name: docker
- Repository URL: Docker's official Ubuntu repository
- GPG Key URL: Docker's public GPG key
- Package to install: docker-ce-cli

## Validation

The `validate.sh` script:
1. Initializes a git repository (required for dottie)
2. Runs `dottie install` with the repository configuration
3. Verifies the repository source file was created
4. Verifies the GPG key file was installed
5. Checks if the package was installed successfully

## Requirements

- Must be run with sudo privileges (for repository setup)
- Requires a system with apt-get (Ubuntu/Debian)
- Internet connectivity for GPG key and package downloads

## Notes

- This test validates APT repository setup with GPG key verification
- Private repos require explicit GPG key handling for security
- The test uses Docker's public repository as a known good example
