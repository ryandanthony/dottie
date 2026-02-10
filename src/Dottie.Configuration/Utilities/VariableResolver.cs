// -----------------------------------------------------------------------
// <copyright file="VariableResolver.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Dottie.Configuration.Models;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Utilities;

/// <summary>
/// Regex-based variable substitution engine that resolves <c>${KEY_NAME}</c> patterns
/// in configuration string values.
/// </summary>
public static partial class VariableResolver
{
    private static readonly HashSet<string> GithubDeferredVariables = new(StringComparer.Ordinal) { "RELEASE_VERSION" };
    private static readonly HashSet<string> AptRepoDeferredVariables = new(StringComparer.Ordinal) { "SIGNING_FILE" };

    /// <summary>
    /// Resolves all <c>${...}</c> variable references in a single string.
    /// </summary>
    /// <param name="input">The string containing zero or more <c>${KEY_NAME}</c> references.</param>
    /// <param name="variables">Available variable name to value mappings.</param>
    /// <param name="deferredVariables">Variable names allowed to remain unresolved without error.</param>
    /// <returns>A <see cref="VariableResolutionResult"/> with the resolved string and any errors.</returns>
    public static VariableResolutionResult ResolveString(
        string input,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlySet<string>? deferredVariables = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new VariableResolutionResult(input ?? string.Empty, []);
        }

        var unresolved = new List<string>();

        var resolved = VariablePattern().Replace(input, match =>
        {
            var varName = match.Groups["name"].Value;

            if (variables.TryGetValue(varName, out var value))
            {
                return value;
            }

            #pragma warning disable MA0002 // IReadOnlySet.Contains has no comparer overload
            if (deferredVariables is not null && deferredVariables.Contains(varName))
            #pragma warning restore MA0002
            {
                // Leave deferred variables as-is
                return match.Value;
            }

            unresolved.Add(varName);
            return match.Value;
        });

        return new VariableResolutionResult(resolved, unresolved);
    }

    /// <summary>
    /// Constructs the global variable dictionary from system information sources.
    /// </summary>
    /// <param name="osReleaseVariables">Variables parsed from <c>/etc/os-release</c>.</param>
    /// <returns>A combined dictionary with OS release and architecture variables.</returns>
    public static IReadOnlyDictionary<string, string> BuildVariableSet(
        IReadOnlyDictionary<string, string> osReleaseVariables)
    {
        ArgumentNullException.ThrowIfNull(osReleaseVariables);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        // Add OS release variables first
        foreach (var kvp in osReleaseVariables)
        {
            result[kvp.Key] = kvp.Value;
        }

        // Architecture variables override any conflicting OS release keys
        // FR-004: Skip "unknown" values so referencing them produces an unresolvable error
        var arch = ArchitectureDetector.RawArchitecture;
        if (!string.Equals(arch, "unknown", StringComparison.Ordinal))
        {
            result["ARCH"] = arch;
        }

        var msArch = ArchitectureDetector.CurrentArchitecture;
        if (!string.Equals(msArch, "unknown", StringComparison.Ordinal))
        {
            result["MS_ARCH"] = msArch;
        }

        return result;
    }

    /// <summary>
    /// Resolves all variables across an entire <see cref="DottieConfiguration"/> object.
    /// </summary>
    /// <param name="configuration">The parsed configuration with unresolved variable references.</param>
    /// <param name="variables">Global variables (OS release + architecture).</param>
    /// <returns>A <see cref="ConfigurationResolutionResult"/> with resolved configuration and any errors.</returns>
    public static ConfigurationResolutionResult ResolveConfiguration(
        DottieConfiguration configuration,
        IReadOnlyDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = new List<VariableResolutionError>();
        var resolvedProfiles = new Dictionary<string, ConfigProfile>(StringComparer.Ordinal);

        foreach (var (profileName, profile) in configuration.Profiles)
        {
            var resolvedProfile = ResolveProfile(profileName, profile, variables, errors);
            resolvedProfiles[profileName] = resolvedProfile;
        }

        var resolvedConfig = configuration with { Profiles = resolvedProfiles };
        return new ConfigurationResolutionResult(resolvedConfig, errors);
    }

    private static ConfigProfile ResolveProfile(
        string profileName,
        ConfigProfile profile,
        IReadOnlyDictionary<string, string> variables,
        List<VariableResolutionError> errors)
    {
        // Resolve dotfile entries (no deferred variables)
        var resolvedDotfiles = ResolveDotfileEntries(profileName, profile.Dotfiles, variables, errors);

        // Resolve install block if present
        var resolvedInstall = profile.Install is not null
            ? ResolveInstallBlock(profileName, profile.Install, variables, errors)
            : null;

        return profile with
        {
            Dotfiles = resolvedDotfiles,
            Install = resolvedInstall,
        };
    }

    private static IList<DotfileEntry> ResolveDotfileEntries(
        string profileName,
        IList<DotfileEntry> dotfiles,
        IReadOnlyDictionary<string, string> variables,
        List<VariableResolutionError> errors)
    {
        var resolved = new List<DotfileEntry>(dotfiles.Count);

        foreach (var entry in dotfiles)
        {
            var sourceResult = ResolveString(entry.Source, variables);
            CollectErrors(errors, profileName, entry.Source, "source", sourceResult);

            var targetResult = ResolveString(entry.Target, variables);
            CollectErrors(errors, profileName, entry.Source, "target", targetResult);

            resolved.Add(entry with
            {
                Source = sourceResult.ResolvedValue,
                Target = targetResult.ResolvedValue,
            });
        }

        return resolved;
    }

    private static InstallBlock ResolveInstallBlock(
        string profileName,
        InstallBlock installBlock,
        IReadOnlyDictionary<string, string> variables,
        List<VariableResolutionError> errors)
    {
        var resolvedAptRepos = ResolveAptRepoItems(profileName, installBlock.AptRepos, variables, errors);
        var resolvedGithub = ResolveGithubReleaseItems(profileName, installBlock.Github, variables, errors);

        return installBlock with
        {
            AptRepos = resolvedAptRepos,
            Github = resolvedGithub,
        };
    }

    private static IList<AptRepoItem> ResolveAptRepoItems(
        string profileName,
        IList<AptRepoItem> aptRepos,
        IReadOnlyDictionary<string, string> variables,
        List<VariableResolutionError> errors)
    {
        var resolved = new List<AptRepoItem>(aptRepos.Count);

        foreach (var item in aptRepos)
        {
            // APT repos have SIGNING_FILE as deferred
            var repoResult = ResolveString(item.Repo, variables, AptRepoDeferredVariables);
            CollectErrors(errors, profileName, item.Name, "repo", repoResult);

            var keyUrlResult = ResolveString(item.KeyUrl, variables);
            CollectErrors(errors, profileName, item.Name, "key_url", keyUrlResult);

            var resolvedPackages = ResolveAptRepoPackages(profileName, item.Name, item.Packages, variables, errors);

            resolved.Add(item with
            {
                Repo = repoResult.ResolvedValue,
                KeyUrl = keyUrlResult.ResolvedValue,
                Packages = resolvedPackages,
            });
        }

        return resolved;
    }

    private static IList<GithubReleaseItem> ResolveGithubReleaseItems(
        string profileName,
        IList<GithubReleaseItem> githubItems,
        IReadOnlyDictionary<string, string> variables,
        List<VariableResolutionError> errors)
    {
        var resolved = new List<GithubReleaseItem>(githubItems.Count);

        foreach (var item in githubItems)
        {
            // GitHub items have RELEASE_VERSION as deferred
            var assetResult = ResolveString(item.Asset, variables, GithubDeferredVariables);
            CollectErrors(errors, profileName, item.Repo, "asset", assetResult);

            VariableResolutionResult? binaryResult = item.Binary is not null
                ? ResolveString(item.Binary, variables, GithubDeferredVariables)
                : null;

            if (binaryResult is not null)
            {
                CollectErrors(errors, profileName, item.Repo, "binary", binaryResult);
            }

            resolved.Add(item with
            {
                Asset = assetResult.ResolvedValue,
                Binary = binaryResult?.ResolvedValue ?? item.Binary,
            });
        }

        return resolved;
    }

    private static IList<string> ResolveAptRepoPackages(
        string profileName,
        string entryIdentifier,
        IList<string> values,
        IReadOnlyDictionary<string, string> variables,
        List<VariableResolutionError> errors)
    {
        var resolved = new List<string>(values.Count);

        foreach (var value in values)
        {
            var result = ResolveString(value, variables, AptRepoDeferredVariables);
            CollectErrors(errors, profileName, entryIdentifier, "packages", result);
            resolved.Add(result.ResolvedValue);
        }

        return resolved;
    }

    private static void CollectErrors(
        List<VariableResolutionError> errors,
        string profileName,
        string entryIdentifier,
        string fieldName,
        VariableResolutionResult result)
    {
        foreach (var varName in result.UnresolvedVariables)
        {
            errors.Add(new VariableResolutionError(
                profileName,
                entryIdentifier,
                fieldName,
                varName,
                $"Unresolvable variable '${{{varName}}}' in profile '{profileName}', entry '{entryIdentifier}', field '{fieldName}'"));
        }
    }

    [GeneratedRegex(@"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VariablePattern();
}
