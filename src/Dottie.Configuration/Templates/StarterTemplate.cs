// -----------------------------------------------------------------------
// <copyright file="StarterTemplate.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Templates;

/// <summary>
/// Generates a starter configuration template for new dottie projects.
/// </summary>
public static class StarterTemplate
{
    /// <summary>
    /// The name of the embedded template resource.
    /// </summary>
    private const string TemplateName = "Dottie.Configuration.Templates.starter-template.yaml";

    /// <summary>
    /// The fallback inline template content.
    /// </summary>
    private const string InlineTemplate = """
                                          # dottie Configuration File
                                          # https://github.com/ryandanthony/dottie
                                          #
                                          # This file defines dotfile symlinks and optional software to install.
                                          # Run 'dottie validate <profile>' to verify your configuration.

                                          profiles:
                                            default:
                                              # Dotfile symlinks to create
                                              dotfiles:
                                                - source: dotfiles/.bashrc
                                                  target: ~/.bashrc
                                                - source: dotfiles/.gitconfig
                                                  target: ~/.gitconfig

                                              # Optional: Software installation
                                              # Uncomment and customize as needed
                                              #
                                              # install:
                                              #   # APT packages (Debian/Ubuntu)
                                              #   apt:
                                              #     - git
                                              #     - curl
                                              #     - vim
                                              #
                                              #   # Shell scripts to run (must be in repo)
                                              #   scripts:
                                              #     - scripts/setup.sh
                                              #
                                              #   # GitHub releases (architecture-aware)
                                              #   github:
                                              #     - repo: owner/repo
                                              #       asset: binary-{arch}.tar.gz
                                              #       binary: binary
                                              #
                                              #   # Snap packages
                                              #   snaps:
                                              #     - name: code
                                              #       classic: true
                                              #
                                              #   # Nerd Fonts
                                              #   fonts:
                                              #     - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.0/FiraCode.zip
                                              #
                                              #   # APT repositories
                                              #   aptRepos:
                                              #     - name: example-repo
                                              #       key_url: https://example.com/key.gpg
                                              #       repo: "deb https://example.com/apt stable main"
                                              #       packages:
                                              #         - package-name

                                            # Example: Additional profile that extends default
                                            # work:
                                            #   extends: default
                                            #   dotfiles:
                                            #     - source: dotfiles/.work-gitconfig
                                            #       target: ~/.gitconfig-work
                                          """;

    /// <summary>
    /// Generates the starter template content.
    /// </summary>
    /// <returns>The YAML content for a starter dottie configuration.</returns>
    public static string Generate()
    {
        var assembly = typeof(StarterTemplate).Assembly;
        using var stream = assembly.GetManifestResourceStream(TemplateName);

        if (stream is null)
        {
            // Fallback to inline template if embedded resource is not found
            return InlineTemplate;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets the default filename for the configuration file.
    /// </summary>
    /// <value>
    /// The default filename for the configuration file.
    /// </value>
    public static string DefaultFileName => "dottie.yaml";

    /// <summary>
    /// Writes the starter template to the specified path.
    /// </summary>
    /// <param name="path">The path to write the template to.</param>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    public static void WriteTo(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var content = Generate();
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// Writes the starter template to the specified directory using the default filename.
    /// </summary>
    /// <param name="directory">The directory to write the template to.</param>
    /// <exception cref="ArgumentNullException">Thrown when directory is null.</exception>
    public static void WriteToDirectory(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        var path = Path.Combine(directory, DefaultFileName);
        WriteTo(path);
    }
}
