using dottie;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Serilog;
using Serilog.Sinks.Spectre;


Log.Logger = args.Contains("--verbose")
    ? new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.Spectre()
        .CreateLogger()
    : new LoggerConfiguration()
        .WriteTo.Spectre()
        .CreateLogger();

var registrations = new ServiceCollection();
registrations.AddSingleton<ILogger>(provider => Log.Logger);

var registrar = new dottie.Infrastructure.TypeRegistrar(registrations);
var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.AddCommand<RunCommand>("run")
        .WithDescription("Run the configuration")
        .WithExample(new []{"~/repos/dottie-files/", "~/"})
        ;
    
});
return app.Run(args);
//Notes:
// Mono.Unix.UnixSymbolicLinkInfo
// https://www.anishathalye.com/2014/08/03/managing-your-dotfiles/
/*
#!/bin/bash

BASEDIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# vim
ln -s ${BASEDIR}/vimrc ~/.vimrc
ln -s ${BASEDIR}/vim/ ~/.vim

# zsh
ln -s ${BASEDIR}/zshrc ~/.zshrc

# git
ln -s ${BASEDIR}/gitconfig ~/.gitconfig

*/