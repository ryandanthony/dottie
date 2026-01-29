// -----------------------------------------------------------------------
// <copyright file="ConfigurationLoader.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;
using Dottie.Configuration.Validation;
using YamlDotNet.Core;

namespace Dottie.Configuration.Parsing;

/// <summary>
/// Loads and parses dottie configuration files.
/// </summary>
public class ConfigurationLoader
{
    /// <summary>
    /// Loads a configuration from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <returns>A <see cref="LoadResult"/> containing the configuration or errors.</returns>
    public LoadResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return new LoadResult
            {
                Errors = [new ValidationError(filePath, $"Configuration file not found: {filePath}")],
            };
        }

        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch (IOException ex)
        {
            return new LoadResult
            {
                Errors = [new ValidationError(filePath, $"Failed to read configuration file: {ex.Message}")],
            };
        }

        return LoadFromString(content, filePath);
    }

    /// <summary>
    /// Loads a configuration from a YAML string.
    /// </summary>
    /// <param name="yamlContent">The YAML content.</param>
    /// <param name="sourcePath">The source path for error reporting.</param>
    /// <returns>A <see cref="LoadResult"/> containing the configuration or errors.</returns>
    public LoadResult LoadFromString(string yamlContent, string? sourcePath = null)
    {
        var deserializer = ConfigurationYamlContext.CreateDeserializer();
        var errorPath = sourcePath ?? "yaml";

        DottieConfiguration? configuration;
        try
        {
            configuration = deserializer.Deserialize<DottieConfiguration>(yamlContent);
        }
        catch (YamlException ex)
        {
            return new LoadResult
            {
                Errors =
                [
                    new ValidationError(
                        errorPath,
                        $"YAML parse error: {ex.Message}",
                        (int)ex.Start.Line,
                        (int)ex.Start.Column),
                ],
            };
        }

        if (configuration is null)
        {
            return new LoadResult
            {
                Errors = [new ValidationError(errorPath, "Configuration file is empty or invalid - must contain 'profiles' key")],
            };
        }

        // Validate profiles exist
        if (configuration.Profiles.Count == 0)
        {
            return new LoadResult
            {
                Errors = [new ValidationError("profiles", "Configuration must contain at least one profile under 'profiles' key")],
            };
        }

        return new LoadResult { Configuration = configuration };
    }
}
