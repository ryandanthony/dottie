using dottie;
using Spectre.Console.Cli;

var app = new CommandApp();
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