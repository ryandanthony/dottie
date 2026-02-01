# Installer Services Contract

**Feature**: 006-install-sources  
**Date**: January 31, 2026

## IInstallSource Interface

All installer services implement this common interface:

```csharp
namespace Dottie.Configuration.Installing;

/// <summary>
/// Contract for services that install items from a specific source type.
/// </summary>
public interface IInstallSource
{
    /// <summary>
    /// Gets the human-readable name for this source (e.g., "GitHub Releases").
    /// </summary>
    string SourceName { get; }
    
    /// <summary>
    /// Gets the installation priority. Lower values are processed first.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Installs items from this source.
    /// </summary>
    /// <param name="installBlock">The install block containing items for this source.</param>
    /// <param name="context">Shared context with paths, capabilities, and settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Results for each item processed.</returns>
    Task<IReadOnlyList<InstallResult>> InstallAsync(
        InstallBlock installBlock,
        InstallContext context,
        CancellationToken cancellationToken = default);
}
```

## Service Implementations

### GithubReleaseInstaller

**Priority**: 1  
**Requires Sudo**: No  
**Network**: Yes

```csharp
public class GithubReleaseInstaller : IInstallSource
{
    public string SourceName => "GitHub Releases";
    public int Priority => 1;
    
    // Dependencies (injected)
    private readonly IHttpDownloader _downloader;
    private readonly IArchiveExtractor _extractor;
}
```

**Behavior**:
1. For each `GithubReleaseItem` in `installBlock.Github`:
2. Query GitHub API for release (latest or specific version)
3. Find asset matching glob pattern (first match if multiple)
4. Download asset with retry logic
5. Extract archive if applicable (.tar.gz, .zip, .tgz)
6. Copy binary to `context.BinDirectory`
7. Set executable permission (chmod +x)

**API Endpoints**:
- Latest release: `GET https://api.github.com/repos/{owner}/{repo}/releases/latest`
- Tagged release: `GET https://api.github.com/repos/{owner}/{repo}/releases/tags/{tag}`

---

### AptPackageInstaller

**Priority**: 2  
**Requires Sudo**: Yes  
**Network**: Yes (via apt)

```csharp
public class AptPackageInstaller : IInstallSource
{
    public string SourceName => "APT Packages";
    public int Priority => 2;
}
```

**Behavior**:
1. If `!context.HasSudo`, return Warning results for all packages
2. Run `sudo apt-get update` (once)
3. For each package in `installBlock.Apt`:
4. Run `sudo apt-get install -y {package}`
5. Return Success or Failed based on exit code

**Commands**:
```bash
sudo apt-get update
sudo apt-get install -y package1 package2 ...
```

---

### AptRepoInstaller

**Priority**: 3  
**Requires Sudo**: Yes  
**Network**: Yes

```csharp
public class AptRepoInstaller : IInstallSource
{
    public string SourceName => "Private APT Repos";
    public int Priority => 3;
}
```

**Behavior**:
1. If `!context.HasSudo`, return Warning results for all repos
2. For each `AptRepoItem` in `installBlock.AptRepos`:
3. Download GPG key from `item.KeyUrl`
4. Add key to `/etc/apt/trusted.gpg.d/{name}.gpg` or use `signed-by`
5. Create `/etc/apt/sources.list.d/{name}.list` with `item.Repo`
6. Run `sudo apt-get update`
7. Install each package in `item.Packages`

**File Paths**:
- GPG key: `/etc/apt/trusted.gpg.d/{name}.gpg`
- Sources list: `/etc/apt/sources.list.d/{name}.list`

---

### ScriptRunner

**Priority**: 4  
**Requires Sudo**: No (scripts may use sudo internally)  
**Network**: Depends on script

```csharp
public class ScriptRunner : IInstallSource
{
    public string SourceName => "Scripts";
    public int Priority => 4;
}
```

**Behavior**:
1. For each script path in `installBlock.Scripts`:
2. Resolve path relative to `context.RepoRoot`
3. Validate path doesn't escape repo root (security check)
4. Validate script file exists
5. Execute with: `bash {script_path}`
6. Working directory: `context.RepoRoot`
7. Return Success/Failed based on exit code

**Security**:
```csharp
var fullPath = Path.GetFullPath(Path.Combine(context.RepoRoot, scriptPath));
if (!fullPath.StartsWith(context.RepoRoot, StringComparison.Ordinal))
{
    return InstallResult.Failed(scriptPath, InstallSourceType.Script, 
        "Script path escapes repository root");
}
```

---

### FontInstaller

**Priority**: 5  
**Requires Sudo**: No  
**Network**: Yes

```csharp
public class FontInstaller : IInstallSource
{
    public string SourceName => "Fonts";
    public int Priority => 5;
    
    private readonly IHttpDownloader _downloader;
    private readonly IArchiveExtractor _extractor;
}
```

**Behavior**:
1. For each `FontItem` in `installBlock.Fonts`:
2. Download archive from `item.Url`
3. Create directory `{context.FontDirectory}/{item.Name}/`
4. Extract archive to font directory
5. After all fonts installed, run `fc-cache -fv`

**Paths**:
- Font directory: `~/.local/share/fonts/{name}/`
- Cache refresh: `fc-cache -fv`

---

### SnapPackageInstaller

**Priority**: 6  
**Requires Sudo**: Yes  
**Network**: Yes (via snap)

```csharp
public class SnapPackageInstaller : IInstallSource
{
    public string SourceName => "Snap Packages";
    public int Priority => 6;
}
```

**Behavior**:
1. If `!context.HasSudo`, return Warning results for all snaps
2. For each `SnapItem` in `installBlock.Snaps`:
3. Build command: `sudo snap install {item.Name} [--classic]`
4. Execute and return Success/Failed based on exit code

**Commands**:
```bash
sudo snap install package-name
sudo snap install package-name --classic
```

---

## InstallOrchestrator

Coordinates all installer services in priority order.

```csharp
public class InstallOrchestrator
{
    private readonly IReadOnlyList<IInstallSource> _sources;
    
    public InstallOrchestrator()
    {
        _sources = new IInstallSource[]
        {
            new GithubReleaseInstaller(),
            new AptPackageInstaller(),
            new AptRepoInstaller(),
            new ScriptRunner(),
            new FontInstaller(),
            new SnapPackageInstaller()
        }.OrderBy(s => s.Priority).ToList();
    }
    
    public async Task<IReadOnlyList<InstallResult>> InstallAsync(
        InstallBlock installBlock,
        InstallContext context,
        IProgress<InstallProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<InstallResult>();
        
        foreach (var source in _sources)
        {
            progress?.Report(new InstallProgress(source.SourceName, InstallPhase.Starting));
            
            var results = await source.InstallAsync(installBlock, context, cancellationToken);
            allResults.AddRange(results);
            
            progress?.Report(new InstallProgress(source.SourceName, InstallPhase.Completed, results));
        }
        
        return allResults;
    }
}
```

## Utility Contracts

### IHttpDownloader

```csharp
public interface IHttpDownloader
{
    /// <summary>
    /// Downloads content from URL with retry logic.
    /// </summary>
    /// <param name="url">URL to download.</param>
    /// <param name="authToken">Optional bearer token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Downloaded content as stream.</returns>
    /// <exception cref="HttpRequestException">After all retries exhausted.</exception>
    Task<Stream> DownloadAsync(
        string url, 
        string? authToken = null,
        CancellationToken cancellationToken = default);
}
```

### IArchiveExtractor

```csharp
public interface IArchiveExtractor
{
    /// <summary>
    /// Extracts archive to specified directory.
    /// </summary>
    /// <param name="archivePath">Path to archive file.</param>
    /// <param name="destinationDirectory">Directory to extract to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExtractAsync(
        string archivePath, 
        string destinationDirectory,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extracts a single file from archive.
    /// </summary>
    /// <param name="archivePath">Path to archive file.</param>
    /// <param name="fileName">Name of file to extract.</param>
    /// <param name="destinationPath">Full path for extracted file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExtractFileAsync(
        string archivePath,
        string fileName,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
```

### ISudoChecker

```csharp
public interface ISudoChecker
{
    /// <summary>
    /// Checks if sudo is available (either running as root or sudo can be invoked).
    /// </summary>
    bool HasSudo();
}
```

## Error Handling Contract

All installers follow this error handling pattern:

1. **Catch and continue**: Individual item failures don't stop other items
2. **Return results**: Every item gets an `InstallResult` (Success, Skipped, Warning, Failed)
3. **Include context**: Error messages include item name and actionable details
4. **Don't throw**: Exceptions are caught and converted to Failed results

```csharp
try
{
    // Installation logic
    return InstallResult.Success(item.Name, SourceType, installedPath);
}
catch (HttpRequestException ex)
{
    return InstallResult.Failed(item.Name, SourceType, 
        $"Download failed: {ex.Message}");
}
catch (Exception ex)
{
    return InstallResult.Failed(item.Name, SourceType, 
        $"Unexpected error: {ex.Message}");
}
```
