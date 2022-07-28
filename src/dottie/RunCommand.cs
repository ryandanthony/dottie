using dottie.Config;
using dottie.Processors;
using dottie.Processors.AptGet;
using dottie.Processors.Links;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dottie;

public class RunCommand : AsyncCommand<RunCommand.Settings>
{
    private readonly ILogger _logger;

    public RunCommand(ILogger logger)
    {
        _logger = logger;
    }
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[DottieDirectory]")]
        public string DottieDirectory { get; set; }

        [CommandArgument(1, "[HomeDirectory]")]
        public string HomeDirectory { get; set; }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!Directory.Exists(settings.DottieDirectory))
        {
            return ValidationResult.Error($"Path not found - {settings.DottieDirectory}");
        }

        var dottieFile = Path.Join(settings.DottieDirectory, "dottie.yaml");
        if (!File.Exists(dottieFile))
        {
            return ValidationResult.Error($"File not found - {dottieFile}");
        }

        if (!Directory.Exists(settings.HomeDirectory))
        {
            return ValidationResult.Error($"Path not found - {settings.HomeDirectory}");
        }

        return base.Validate(context, settings);
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine($"Home Directory: [blue]{settings.HomeDirectory}[/]");
        AnsiConsole.MarkupLine($"Dottie Directory: [blue]{settings.DottieDirectory}[/]");
        var dottieFile = Path.Join(settings.DottieDirectory, "dottie.yaml");
        AnsiConsole.MarkupLine($"Dottie File: [blue]{dottieFile}[/]");


        // var config = new Configuration()
        // {
        //     Links = new Dictionary<string, LinkSettings>()
        //     {
        //         { "~/.vimrc", new LinkSettings() { Force = true, Target = "vimrc" } },
        //         { "~/.vim", new LinkSettings() { Force = true, Target = "vim/" } }
        //     },
        //     AptGet = new List<AptVersion>()
        //     {
        //         new() { Package = "azure-cli", Version = "2.18.0-2" }
        //     }
        // };
        // var serializer = new SerializerBuilder()
        //     .WithNamingConvention(CamelCaseNamingConvention.Instance)
        //     .Build();
        // var yaml = serializer.Serialize(config);
        //
        // Console.WriteLine(yaml);

        //


        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        Configuration configuration;
        try
        {
            configuration = deserializer.Deserialize<Configuration>(await File.ReadAllTextAsync(dottieFile));
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unable to deserialize dottie file");
            return 1;
        }


        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var processors = new List<IProcessor>()
                {
                    new LinkProcessor(_logger, settings.HomeDirectory, settings.DottieDirectory, configuration.Links),

                    new AptProcessor(_logger, settings.DottieDirectory, configuration.Apt),
                    //new DoNothingProcessor()
                };
                foreach (var processor in processors)
                {
                     var task = ctx.AddTask($"[green]{processor.Name} - Starting[/]").MaxValue(1).Value(0);
                    task.StartTask();
                    processor.Progress += (sender, progress) =>
                    {
                        task.Description = $"[green]{processor.Name} - {progress.CurrentItem}[/]";
                        task.Value = Convert.ToDouble(progress.TotalPercentComplete);
                        //task.IsIndeterminate();
                    };
                    await processor.Run();
                }
            });
         
        return 0;
    }
}