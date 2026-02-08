// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Utilities;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Utilities;

/// <summary>
/// Tests for <see cref="OsReleaseParser"/>.
/// </summary>
public class OsReleaseParserTests
{
    [Fact]
    public void Parse_StandardUbuntuContent_ReturnsExpectedKeyValuePairs()
    {
        // Arrange
        var content = """
            PRETTY_NAME="Ubuntu 24.04.1 LTS"
            NAME="Ubuntu"
            VERSION_ID="24.04"
            VERSION="24.04.1 LTS (Noble Numbat)"
            VERSION_CODENAME=noble
            ID=ubuntu
            ID_LIKE=debian
            HOME_URL="https://www.ubuntu.com/"
            SUPPORT_URL="https://help.ubuntu.com/"
            BUG_REPORT_URL="https://bugs.launchpad.net/ubuntu/"
            PRIVACY_POLICY_URL="https://www.ubuntu.com/legal/terms-and-policies/privacy-policy"
            UBUNTU_CODENAME=noble
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().ContainKey("PRETTY_NAME").WhoseValue.Should().Be("Ubuntu 24.04.1 LTS");
        result.Should().ContainKey("NAME").WhoseValue.Should().Be("Ubuntu");
        result.Should().ContainKey("VERSION_ID").WhoseValue.Should().Be("24.04");
        result.Should().ContainKey("VERSION_CODENAME").WhoseValue.Should().Be("noble");
        result.Should().ContainKey("ID").WhoseValue.Should().Be("ubuntu");
        result.Should().ContainKey("ID_LIKE").WhoseValue.Should().Be("debian");
        result.Should().ContainKey("UBUNTU_CODENAME").WhoseValue.Should().Be("noble");
    }

    [Fact]
    public void Parse_DoubleQuotedValues_StripsQuotes()
    {
        // Arrange
        var content = """
            NAME="Ubuntu"
            VERSION="24.04.1 LTS (Noble Numbat)"
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().ContainKey("NAME").WhoseValue.Should().Be("Ubuntu");
        result.Should().ContainKey("VERSION").WhoseValue.Should().Be("24.04.1 LTS (Noble Numbat)");
    }

    [Fact]
    public void Parse_SingleQuotedValues_StripsQuotes()
    {
        // Arrange
        var content = """
            NAME='Ubuntu'
            VERSION='24.04'
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().ContainKey("NAME").WhoseValue.Should().Be("Ubuntu");
        result.Should().ContainKey("VERSION").WhoseValue.Should().Be("24.04");
    }

    [Fact]
    public void Parse_UnquotedValues_ReturnsValuesAsIs()
    {
        // Arrange
        var content = """
            ID=ubuntu
            VERSION_CODENAME=noble
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().ContainKey("ID").WhoseValue.Should().Be("ubuntu");
        result.Should().ContainKey("VERSION_CODENAME").WhoseValue.Should().Be("noble");
    }

    [Fact]
    public void Parse_CommentLines_AreIgnored()
    {
        // Arrange
        var content = """
            # This is a comment
            ID=ubuntu
            # Another comment
            NAME="Ubuntu"
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("ID");
        result.Should().ContainKey("NAME");
    }

    [Fact]
    public void Parse_BlankLines_AreIgnored()
    {
        // Arrange
        var content = "ID=ubuntu\n\n\nNAME=\"Ubuntu\"\n\n";

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("ID");
        result.Should().ContainKey("NAME");
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsEmptyDictionary()
    {
        // Arrange
        var content = string.Empty;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MalformedLinesNoEquals_AreIgnored()
    {
        // Arrange
        var content = """
            ID=ubuntu
            this line has no equals sign
            NAME="Ubuntu"
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("ID");
        result.Should().ContainKey("NAME");
    }

    [Fact]
    public void Parse_DuplicateKeys_LastValueWins()
    {
        // Arrange
        var content = """
            ID=ubuntu
            ID=debian
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().ContainKey("ID").WhoseValue.Should().Be("debian");
    }

    [Fact]
    public void Parse_KeysWithEmptyValues_PreservedAsEmpty()
    {
        // Arrange
        var content = """
            ID=
            NAME=""
            VERSION=''
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().ContainKey("ID").WhoseValue.Should().Be(string.Empty);
        result.Should().ContainKey("NAME").WhoseValue.Should().Be(string.Empty);
        result.Should().ContainKey("VERSION").WhoseValue.Should().Be(string.Empty);
    }

    [Fact]
    public void Parse_ValueWithMultipleEquals_SplitsOnFirstOnly()
    {
        // Arrange
        var content = """
            BUG_REPORT_URL="https://bugs.example.com/?param=value"
            """;

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().ContainKey("BUG_REPORT_URL")
            .WhoseValue.Should().Be("https://bugs.example.com/?param=value");
    }

    [Fact]
    public void TryReadFromSystem_WithExistingFile_ReturnsVariablesAndIsAvailable()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "ID=ubuntu\nVERSION_CODENAME=noble\n");

            // Act
            var (variables, isAvailable) = OsReleaseParser.TryReadFromSystem(tempFile);

            // Assert
            isAvailable.Should().BeTrue();
            variables.Should().ContainKey("ID").WhoseValue.Should().Be("ubuntu");
            variables.Should().ContainKey("VERSION_CODENAME").WhoseValue.Should().Be("noble");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void TryReadFromSystem_WithNonExistentFile_ReturnsEmptyAndNotAvailable()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "os-release");

        // Act
        var (variables, isAvailable) = OsReleaseParser.TryReadFromSystem(nonExistentPath);

        // Assert
        isAvailable.Should().BeFalse();
        variables.Should().BeEmpty();
    }

    [Fact]
    public void Parse_NullContent_ReturnsEmptyDictionary()
    {
        // Act
        var result = OsReleaseParser.Parse(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhitespaceOnlyContent_ReturnsEmptyDictionary()
    {
        // Arrange
        var content = "   \n  \n   ";

        // Act
        var result = OsReleaseParser.Parse(content);

        // Assert
        result.Should().BeEmpty();
    }
}
