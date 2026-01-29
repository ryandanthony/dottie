// -----------------------------------------------------------------------
// <copyright file="ConfigurationYamlContext.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dottie.Configuration.Parsing;

/// <summary>
/// Provides YAML serialization context for configuration types.
/// Configured for AOT/trimming compatibility.
/// </summary>
public static class ConfigurationYamlContext
{
    /// <summary>
    /// Gets the configured YAML deserializer for reading dottie.yaml files.
    /// </summary>
    /// <returns>A configured <see cref="IDeserializer"/> instance.</returns>
    public static IDeserializer CreateDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Gets the configured YAML serializer for writing dottie.yaml files.
    /// </summary>
    /// <returns>A configured <see cref="ISerializer"/> instance.</returns>
    public static ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
    }
}
