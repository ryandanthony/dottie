# Integration Test Container
# Purpose: Validate dottie CLI behavior on Ubuntu with real configuration scenarios
#
# This image is designed to run individual test scenarios in isolated environments.
# Dottie is built once (outside Docker) and copied in; each test runs in its own /tmp directory.
#
# Usage (from repo root):
#   dotnet publish src/Dottie.Cli -c Release -r linux-x64 --self-contained -o ./publish/linux-x64
#   docker build -t dottie-integration-test -f tests/integration/Dockerfile .
#   docker run --rm dottie-integration-test
#   docker run --rm -e TEST_NAME=basic-symlinks dottie-integration-test
#   docker run --rm -e VERBOSITY=Verbose dottie-integration-test

FROM ubuntu:24.04

# Install common dependencies that dottie might need to interact with
RUN apt-get update && apt-get install -y \
    curl \
    git \
    sudo \
    jq \
    && rm -rf /var/lib/apt/lists/*

# Create test user (non-root for realistic testing)
RUN useradd -m -s /bin/bash testuser && \
    echo "testuser ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Copy dottie binary (from publish output, context is repo root)
COPY publish/linux-x64/dottie /usr/local/bin/dottie
RUN chmod +x /usr/local/bin/dottie

# Copy test scenarios and validation scripts (paths relative to repo root)
COPY tests/integration/scenarios/ /home/testuser/scenarios/
COPY tests/integration/scripts/ /home/testuser/scripts/

# Set ownership
RUN chown -R testuser:testuser /home/testuser

# Create a directory for test runs (isolated per execution)
RUN mkdir -p /tmp/test-runs && chown testuser:testuser /tmp/test-runs

# Switch to test user
USER testuser
WORKDIR /home/testuser

# Environment variables for test configuration
ENV VERBOSITY=Normal
ENV TEST_NAME=

# Run validation script
ENTRYPOINT ["/bin/bash", "/home/testuser/scripts/run-scenarios.sh"]
