// -----------------------------------------------------------------------
// <copyright file="ProfileMerger.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Inheritance;

/// <summary>
/// Merges profile configurations following inheritance rules.
/// </summary>
public sealed class ProfileMerger
{
    private readonly DottieConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileMerger"/> class.
    /// </summary>
    /// <param name="configuration">The configuration containing all profiles.</param>
    public ProfileMerger(DottieConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Resolves a profile with all its inheritance chain merged.
    /// </summary>
    /// <param name="profileName">The name of the profile to resolve.</param>
    /// <returns>The resolved profile or an error.</returns>
    public InheritanceResolveResult Resolve(string profileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        // Handle implicit default profile - when 'default' is requested but not defined
        if (string.Equals(profileName, "default", StringComparison.Ordinal) &&
            !_configuration.Profiles.ContainsKey(profileName))
        {
            return InheritanceResolveResult.Success(new ResolvedProfile
            {
                Name = profileName,
                Dotfiles = [],
                Install = null,
                InheritanceChain = [profileName],
            });
        }

        if (!_configuration.Profiles.ContainsKey(profileName))
        {
            return InheritanceResolveResult.Failure($"Profile '{profileName}' not found.");
        }

        // Build inheritance chain and detect cycles
        var chainResult = BuildInheritanceChain(profileName);
        if (!chainResult.IsSuccess)
        {
            return InheritanceResolveResult.Failure(chainResult.Error!);
        }

        var chain = chainResult.Chain!;

        // Merge profiles from ancestor to descendant
        var mergedDotfiles = new List<DotfileEntry>();
        InstallBlock? mergedInstall = null;

        foreach (var name in chain)
        {
            var currentProfile = _configuration.Profiles[name];
            mergedDotfiles = MergeDotfiles(mergedDotfiles, currentProfile.Dotfiles);
            mergedInstall = MergeInstallBlocks(mergedInstall, currentProfile.Install);
        }

        return InheritanceResolveResult.Success(new ResolvedProfile
        {
            Name = profileName,
            Dotfiles = mergedDotfiles,
            Install = mergedInstall,
            InheritanceChain = chain,
        });
    }

    /// <summary>
    /// Merges dotfiles lists by target path deduplication.
    /// Child entries override parent entries with the same target path.
    /// </summary>
    /// <param name="parent">The parent profile's dotfiles.</param>
    /// <param name="child">The child profile's dotfiles.</param>
    /// <returns>A merged list with child entries taking precedence for duplicate targets.</returns>
    internal static List<DotfileEntry> MergeDotfiles(IList<DotfileEntry> parent, IList<DotfileEntry> child)
    {
        var merged = new Dictionary<string, DotfileEntry>(StringComparer.Ordinal);

        // Add parent entries first
        foreach (var entry in parent)
        {
            merged[entry.Target] = entry;
        }

        // Child entries override parent entries with same target
        foreach (var entry in child)
        {
            merged[entry.Target] = entry;
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// Merges install blocks following the merge rules:
    /// - Plain lists (apt, scripts) are appended
    /// - Keyed items (github, snap, apt-repo, fonts) are merged by identifier.
    /// </summary>
    internal static InstallBlock? MergeInstallBlocks(InstallBlock? parent, InstallBlock? child)
    {
        if (parent is null)
        {
            return child;
        }

        if (child is null)
        {
            return parent;
        }

        return new InstallBlock
        {
            Apt = MergeLists(parent.Apt, child.Apt),
            Scripts = MergeLists(parent.Scripts, child.Scripts),
            Github = MergeByKey(parent.Github, child.Github, g => g.Repo),
            Snaps = MergeByKey(parent.Snaps, child.Snaps, s => s.Name),
            AptRepos = MergeByKey(parent.AptRepos, child.AptRepos, a => a.Name),
            Fonts = MergeByKey(parent.Fonts, child.Fonts, f => f.Name),
        };
    }

    private static IList<string> MergeLists(IList<string> parent, IList<string> child)
    {
        var result = new List<string>(parent);
        result.AddRange(child);
        return result;
    }

    private static IList<T> MergeByKey<T>(IList<T> parent, IList<T> child, Func<T, string> keySelector)
    {
        var merged = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (var item in parent)
        {
            merged[keySelector(item)] = item;
        }

        foreach (var item in child)
        {
            merged[keySelector(item)] = item;
        }

        return merged.Values.ToList();
    }

    private InheritanceChainResult BuildInheritanceChain(string profileName)
    {
        var chain = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = profileName;

        while (current is not null)
        {
            if (visited.Contains(current))
            {
                return InheritanceChainResult.Failure(
                    $"Circular inheritance detected: {string.Join(" -> ", chain)} -> {current}");
            }

            if (!_configuration.Profiles.TryGetValue(current, out var profile))
            {
                return InheritanceChainResult.Failure(
                    $"Profile '{current}' not found (extended by '{chain[^1]}').");
            }

            visited.Add(current);
            chain.Add(current);
            current = profile.Extends;
        }

        // Reverse so ancestors come first
        chain.Reverse();
        return InheritanceChainResult.Success(chain);
    }

    private sealed record InheritanceChainResult
    {
        public IReadOnlyList<string>? Chain { get; init; }

        public string? Error { get; init; }

        public bool IsSuccess => Chain is not null && Error is null;

        public static InheritanceChainResult Success(IReadOnlyList<string> chain) =>
            new() { Chain = chain };

        public static InheritanceChainResult Failure(string error) =>
            new() { Error = error };
    }
}
