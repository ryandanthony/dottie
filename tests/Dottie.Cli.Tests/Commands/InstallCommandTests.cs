// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Commands;
using FluentAssertions;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="InstallCommand"/>.
/// </summary>
public class InstallCommandTests
{
    [Fact]
    public void InstallCommand_CanBeInstantiated()
    {
        // Act
        var command = new InstallCommand();

        // Assert
        command.Should().NotBeNull();
    }
}
