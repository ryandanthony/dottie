// -----------------------------------------------------------------------
// <copyright file="LinkingOrchestrator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Models;

namespace Dottie.Configuration.Linking;

/// <summary>
/// Orchestrates the linking process for dotfile entries.
/// </summary>
public sealed class LinkingOrchestrator
{
    private readonly ConflictDetector _conflictDetector;
    private readonly BackupService _backupService;
    private readonly SymlinkService _symlinkService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkingOrchestrator"/> class.
    /// </summary>
    /// <param name="conflictDetector">The conflict detector to use.</param>
    /// <param name="backupService">The backup service to use.</param>
    /// <param name="symlinkService">The symlink service to use.</param>
    public LinkingOrchestrator(
        ConflictDetector? conflictDetector = null,
        BackupService? backupService = null,
        SymlinkService? symlinkService = null)
    {
        _conflictDetector = conflictDetector ?? new ConflictDetector();
        _backupService = backupService ?? new BackupService();
        _symlinkService = symlinkService ?? new SymlinkService();
    }

    /// <summary>
    /// Executes the linking process for the given resolved profile.
    /// </summary>
    /// <param name="profile">The resolved profile containing dotfiles to link.</param>
    /// <param name="repoRoot">The repository root path.</param>
    /// <param name="force">Whether to force linking by backing up conflicts.</param>
    /// <returns>The result of the link operation.</returns>
    public LinkExecutionResult ExecuteLink(ResolvedProfile profile, string repoRoot, bool force)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);

        var dotfiles = profile.Dotfiles.ToList().AsReadOnly();
        var conflictResult = _conflictDetector.DetectConflicts(dotfiles, repoRoot);

        if (conflictResult.HasConflicts && !force)
        {
            return LinkExecutionResult.Blocked(conflictResult);
        }

        return ProcessLinking(conflictResult, repoRoot, force);
    }

    private LinkExecutionResult ProcessLinking(ConflictResult conflictResult, string repoRoot, bool force)
    {
        var successfulLinks = new List<LinkResult>();
        var skippedLinks = new List<LinkResult>();
        var failedLinks = new List<LinkResult>();
        var backupResults = new List<BackupResult>();

        ProcessAlreadyLinkedEntries(conflictResult.AlreadyLinked, skippedLinks);

        if (force)
        {
            ProcessConflictsWithForce(conflictResult.Conflicts, repoRoot, successfulLinks, failedLinks, backupResults);
        }

        ProcessSafeEntries(conflictResult.SafeEntries, repoRoot, successfulLinks, failedLinks);

        var linkResult = new LinkOperationResult
        {
            SuccessfulLinks = successfulLinks,
            SkippedLinks = skippedLinks,
            FailedLinks = failedLinks,
        };

        return LinkExecutionResult.Completed(linkResult, backupResults);
    }

    private static void ProcessAlreadyLinkedEntries(
        IReadOnlyList<DotfileEntry> alreadyLinked,
        List<LinkResult> skippedLinks)
    {
        foreach (var entry in alreadyLinked)
        {
            var targetPath = ExpandPath(entry.Target);
            skippedLinks.Add(LinkResult.Skipped(entry, targetPath));
        }
    }

    private void ProcessConflictsWithForce(
        IReadOnlyList<Conflict> conflicts,
        string repoRoot,
        List<LinkResult> successfulLinks,
        List<LinkResult> failedLinks,
        List<BackupResult> backupResults)
    {
        foreach (var conflict in conflicts)
        {
            ProcessSingleConflict(conflict, repoRoot, successfulLinks, failedLinks, backupResults);
        }
    }

    private void ProcessSingleConflict(
        Conflict conflict,
        string repoRoot,
        List<LinkResult> successfulLinks,
        List<LinkResult> failedLinks,
        List<BackupResult> backupResults)
    {
        var backupResult = _backupService.Backup(conflict.TargetPath);
        backupResults.Add(backupResult);

        if (!backupResult.IsSuccess)
        {
            failedLinks.Add(LinkResult.Failure(
                conflict.Entry,
                conflict.TargetPath,
                $"Backup failed: {backupResult.Error}"));
            return;
        }

        var sourcePath = Path.Combine(repoRoot, conflict.Entry.Source);
        if (_symlinkService.CreateSymlink(conflict.TargetPath, sourcePath))
        {
            successfulLinks.Add(LinkResult.Success(conflict.Entry, conflict.TargetPath, backupResult));
        }
        else
        {
            failedLinks.Add(LinkResult.Failure(
                conflict.Entry,
                conflict.TargetPath,
                "Failed to create symlink"));
        }
    }

    private void ProcessSafeEntries(
        IReadOnlyList<DotfileEntry> safeEntries,
        string repoRoot,
        List<LinkResult> successfulLinks,
        List<LinkResult> failedLinks)
    {
        foreach (var entry in safeEntries)
        {
            var targetPath = ExpandPath(entry.Target);
            var sourcePath = Path.Combine(repoRoot, entry.Source);

            if (_symlinkService.CreateSymlink(targetPath, sourcePath))
            {
                successfulLinks.Add(LinkResult.Success(entry, targetPath));
            }
            else
            {
                failedLinks.Add(LinkResult.Failure(entry, targetPath, "Failed to create symlink"));
            }
        }
    }

    private static string ExpandPath(string path)
    {
        if (path.StartsWith('~'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        return Path.GetFullPath(path);
    }
}
