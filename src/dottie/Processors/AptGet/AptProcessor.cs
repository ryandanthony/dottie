using System.Net;
using System.Text;
using CliWrap;
using dottie.Config;
using Serilog;

namespace dottie.Processors.AptGet;

public class AptProcessor : IProcessor
{
    private readonly ILogger _logger;
    private readonly string _tempDirectory;
    private readonly AptConfiguration _configuration;

    public AptProcessor(ILogger logger, string dottieDirectory, AptConfiguration configuration)
    {
        _logger = logger;
        _tempDirectory = Path.Join(dottieDirectory, "temp");
        _configuration = configuration ?? throw new Exception("Invalid Configuration");
    }

    public event EventHandler<ProcessProgress>? Progress;
    public string Name => "Apt";

    public async Task<Status> Run()
    {
        var total = _configuration.PreReqs?.Count ?? 0
            + _configuration.Sources?.Count ?? 0
            + _configuration.Packages?.Count ?? 0;

        var position = 0;
        if (_configuration.PreReqs != null)
        {
            foreach (var item in _configuration.PreReqs)
            {
                position++;
                var result = decimal.Divide(position, total);
                OnProgress(new ProcessProgress()
                {
                    CurrentItem = $"Processing Package {item.Package}",
                    TotalPercentComplete = result
                });
                await Execute(item);
            }
        }

        if (_configuration.Sources != null)
        {
            foreach (var item in _configuration.Sources)
            {
                position++;
                var result = decimal.Divide(position, total);
                OnProgress(new ProcessProgress()
                {
                    CurrentItem = $"Processing Source: {item.Name}",
                    TotalPercentComplete = result
                });
                await Execute(item);
            }

        }

        if (_configuration.Packages != null)
        {

            foreach (var item in _configuration.Packages)
            {
                position++;
                var result = decimal.Divide(position, total);
                OnProgress(new ProcessProgress()
                {
                    CurrentItem = $"Processing Package: {item.Package}",
                    TotalPercentComplete = result
                });
                await Execute(item);
            }
        }

        OnProgress(new ProcessProgress() { CurrentItem = $"Done", TotalPercentComplete = 1 });
        return new Status() { Successful = true };
    }

    private async Task Execute(AptSource item)
    {
        const string keyringDirectory = "/usr/share/keyrings/";
        const string sourcesDirectory = "/etc/apt/sources.list.d/";
        var architecture = await GetArchitecture();

        var keyRingName = item.SigningKeyUrl.ComputeSha256Hash();
        
//_dottieDirectory


         var keyRingFile = new FileInfo(Path.Join(keyringDirectory, $"{keyRingName}.gpg"));
        // if (keyRingFile.Exists)
        // {
        //     _logger.Verbose("Keyring already exists:{FullName}", keyRingFile.FullName);
        // }
        // else
        // {
            var tempDirectoryInfo = new DirectoryInfo(_tempDirectory);
            if (!tempDirectoryInfo.Exists)
            {
                tempDirectoryInfo.Create();
            }
            
            var tempKeyRingFile = new FileInfo(Path.Join(tempDirectoryInfo.FullName, $"{keyRingName}.gpg"));
            var httpClient = new HttpClient();
            await httpClient.DownloadFileAsync(item.SigningKeyUrl, tempKeyRingFile.FullName);

            
            
            //Download
            //Dearmor
            //Install
        // }
        /*
# /usr/bin/env bash
set -e
if [ -f /usr/share/keyrings/virtualbox-archive-keyring.gpg ]
then
    echo "virtualbox gpg already installed"       
else
    curl https://www.virtualbox.org/download/oracle_vbox_2016.asc | gpg --dearmor > virtualbox.gpg
    install -o root -g root -m 644 virtualbox.gpg /usr/share/keyrings/virtualbox-archive-keyring.gpg
    rm virtualbox.gpg
fi
*/
        // var sourcesFile = new FileInfo(Path.Join(sourcesDirectory, $"{item.Name}.list"));
        // if (sourcesFile.Exists)
        // {
        //     _logger.Verbose("Sources file already exists, overwriting:{FullName}", sourcesFile.FullName);
        // }
        //
        // var components = string.Join(" ", item.Components);
        // var data = $"deb [{architecture} signed-by={keyRingFile.FullName}] {item.RepositoryUrl} {item.Distribution} {components}";
        // await File.WriteAllTextAsync(sourcesFile.FullName, data);
        //
        

//https://linuxhint.com/debian_sources-list/


/*
if [ -f /etc/apt/sources.list.d/virtualbox.list ]
then
    echo "virtualbox source list already installed"    
else
    
    sh -c 'echo "deb [arch=amd64 signed-by=/usr/share/keyrings/virtualbox-archive-keyring.gpg] https://download.virtualbox.org/virtualbox/debian $(lsb_release -cs) contrib" > /etc/apt/sources.list.d/virtualbox.list'
fi
 
 */





        await Task.Delay(1);
    }

    private async Task<string> GetArchitecture()
    {
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var arguments = @" -q DEB_BUILD_ARCH";
        var result = await Cli.Wrap("dpkg-architecture")
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteAsync();

        var arch = stdOutBuffer.ToString().Replace(Environment.NewLine, "");
        _logger.Verbose("Architecture: {Arch}", arch);
        return arch;
    }

    private async Task Execute(AptPackage item)
    {
        if (await IsInstalled(item))
        {
            _logger.Verbose("{Package}{Version} is installed. Skipping.", item.Package, item.Version);
            return;
        }
        _logger.Verbose("Installing {Package}{Version}", item.Package, item.Version);
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var arguments = string.IsNullOrWhiteSpace(item.Version)
            ? $"install {item.Package}"
            : $"install {item.Package}={item.Version}";

        var result = await Cli.Wrap("apt-get")
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

// Contains stdOut/stdErr buffered in-memory as string
        var stdOut = stdOutBuffer.ToString();
        var stdErr = stdErrBuffer.ToString();

        _logger.Verbose("\t ExitCode: {ExitCode}", result.ExitCode);
        _logger.Verbose("\t StandardOut: {stdOut}", stdOut);
        if (stdErr.Any())
        {
            _logger.Verbose("\t StandardError: {stdErr}", stdErr);
            if (stdErr.Contains("Permission denied"))
            {
                _logger.Error("Failed to install {Package}. No permissions.", item.Package);
            }

            if (stdErr.Contains("was not found") || stdOut.Contains("was not found"))
            {
                _logger.Error("Failed to install {Package}. Version [{Version}] not found.", item.Package,
                    item.Version);
            }
        }
        _logger.Verbose("Installed {Package}{Version}", item.Package, item.Version);
    }

    private async Task<bool> IsInstalled(AptPackage item)
    {
        //TODO: Rewrite using something else. 

        //apt -qq list awscli
        //awscli/stable 1.19.1-1 all

        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();
        _logger.Verbose("Running apt -qq list {Package}", item.Package);
        var result = await Cli.Wrap("apt")
            .WithArguments(args => args
                .Add("-qq")
                .Add("list")
                .Add(item.Package)
            )
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
        var stdOut = stdOutBuffer.ToString();
        var stdErr = stdErrBuffer.ToString();
        _logger.Verbose("\t ExitCode: {ExitCode}", result.ExitCode);
        _logger.Verbose("\t StandardOut: {stdOut}", stdOut);
        if (stdErr.Any())
        {
            _logger.Verbose("\t StandardError: {stdErr}", stdErr);
        }

        var split = stdOut?.Split(" ");
        if (split?.Length > 1)
        {
            return split[1] == item.Version;
        }

        return false;
    }

    private void OnProgress(ProcessProgress processProgress)
    {
        Progress?.Invoke(this, processProgress);
    }
}