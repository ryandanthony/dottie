// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models;
using Dottie.Configuration.Parsing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Command to install tools from configured sources.
/// </summary>
public sealed class InstallCommand : AsyncCommand<InstallCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, InstallCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            return 1;
        }

        var configPath = settings.ConfigPath ?? Path.Combine(repoRoot, "dottie.yaml");
        if (!File.Exists(configPath))
        {
            var renderer = new InstallProgressRenderer();
            renderer.RenderError("Could not find dottie.yaml in the repository root.");
            return 1;
        }

        try
        {
            var loader = new ConfigurationLoader();
            var loadResult = loader.Load(configPath);

            if (!loadResult.IsSuccess)
            {
                var renderer = new InstallProgressRenderer();
                renderer.RenderError("Failed to load configuration.");
                return 1;
            }

            var profileName = settings.ProfileName ?? "default";
            var profile = loadResult.Configuration?.Profiles?.ContainsKey(profileName) == true 
                ? loadResult.Configuration.Profiles[profileName] 
                : null;

            if (profile?.Install is null)
            {
                var renderer = new InstallProgressRenderer();
                renderer.RenderError($"No install block found in profile '{profileName}'.");
                return 1;
            }

            var context_info = new InstallContext
            {
                RepoRoot = repoRoot,
                HasSudo = SudoChecker.IsSudoAvailable(),
                DryRun = settings.DryRun
            };

            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine("[yellow]Dry Run Mode:[/] Previewing installation without making changes");
            }

            // For now, just return success
            AnsiConsole.MarkupLine("[green]✓[/] Installation ready (implementation in progress)");
            return 0;
        }
        catch (Exception ex)
        {
            var renderer = new InstallProgressRenderer();
            renderer.RenderError($"Installation failed: {ex.Message}");
            return 1;
        }
    }

    private static string? FindRepoRoot()
    {
        var repoRoot = RepoRootFinder.Find();
        if (repoRoot is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not find git repository root.");
            AnsiConsole.MarkupLine("[yellow]Hint:[/] Make sure you're running from within a git repository.");
        }

        return repoRoot;
    }
}
