// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Parsing;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Parsing;

/// <summary>
/// Tests for <see cref="ConfigurationYamlContext"/>.
/// </summary>
public class ConfigurationYamlContextTests
{
    [Fact]
    public void CreateDeserializer_ReturnsValidDeserializer()
    {
        // Act
        var deserializer = ConfigurationYamlContext.CreateDeserializer();

        // Assert
        deserializer.Should().NotBeNull();
    }

    [Fact]
    public void CreateDeserializer_CanDeserializeYaml()
    {
        // Arrange
        var yaml = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
";
        var deserializer = ConfigurationYamlContext.CreateDeserializer();

        // Act
        var result = deserializer.Deserialize<Dictionary<string, object>>(yaml);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("profiles");
    }

    [Fact]
    public void CreateDeserializer_UsesCamelCaseNaming()
    {
        // Arrange
        var yaml = @"
testValue: 123
anotherValue: hello
";
        var deserializer = ConfigurationYamlContext.CreateDeserializer();

        // Act
        var result = deserializer.Deserialize<Dictionary<string, object>>(yaml);

        // Assert
        result.Should().ContainKey("testValue");
        result.Should().ContainKey("anotherValue");
    }

    [Fact]
    public void CreateDeserializer_IgnoresUnmatchedProperties()
    {
        // Arrange
        var yaml = @"
knownProperty: value1
unknownProperty: value2
";
        var deserializer = ConfigurationYamlContext.CreateDeserializer();

        // Act - Should not throw for unknown properties
        var action = () => deserializer.Deserialize<TestClass>(yaml);

        // Assert
        action.Should().NotThrow();
        var result = action();
        result.KnownProperty.Should().Be("value1");
    }

    [Fact]
    public void CreateSerializer_ReturnsValidSerializer()
    {
        // Act
        var serializer = ConfigurationYamlContext.CreateSerializer();

        // Assert
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void CreateSerializer_CanSerializeObjects()
    {
        // Arrange
        var data = new TestClass { KnownProperty = "test value" };
        var serializer = ConfigurationYamlContext.CreateSerializer();

        // Act
        var yaml = serializer.Serialize(data);

        // Assert
        yaml.Should().NotBeNullOrEmpty();
        yaml.Should().Contain("knownProperty");
        yaml.Should().Contain("test value");
    }

    [Fact]
    public void CreateSerializer_UsesCamelCaseNaming()
    {
        // Arrange
        var data = new { TestValue = 123, AnotherValue = "hello" };
        var serializer = ConfigurationYamlContext.CreateSerializer();

        // Act
        var yaml = serializer.Serialize(data);

        // Assert
        yaml.Should().Contain("testValue");
        yaml.Should().Contain("anotherValue");
    }

    [Fact]
    public void CreateSerializer_OmitsNullValues()
    {
        // Arrange
        var data = new TestClass { KnownProperty = "test", NullableProperty = null };
        var serializer = ConfigurationYamlContext.CreateSerializer();

        // Act
        var yaml = serializer.Serialize(data);

        // Assert
        yaml.Should().Contain("knownProperty");
        yaml.Should().NotContain("nullableProperty");
    }

    [Fact]
    public void CreateSerializer_IncludesNonNullValues()
    {
        // Arrange
        var data = new TestClass { KnownProperty = "test", NullableProperty = "not null" };
        var serializer = ConfigurationYamlContext.CreateSerializer();

        // Act
        var yaml = serializer.Serialize(data);

        // Assert
        yaml.Should().Contain("knownProperty");
        yaml.Should().Contain("nullableProperty");
        yaml.Should().Contain("not null");
    }

    [Fact]
    public void Serializer_And_Deserializer_AreCompatible()
    {
        // Arrange
        var original = new TestClass { KnownProperty = "test value", NullableProperty = "optional" };
        var serializer = ConfigurationYamlContext.CreateSerializer();
        var deserializer = ConfigurationYamlContext.CreateDeserializer();

        // Act
        var yaml = serializer.Serialize(original);
        var deserialized = deserializer.Deserialize<TestClass>(yaml);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.KnownProperty.Should().Be(original.KnownProperty);
        deserialized.NullableProperty.Should().Be(original.NullableProperty);
    }

    private class TestClass
    {
        public string? KnownProperty { get; set; }

        public string? NullableProperty { get; set; }
    }
}
