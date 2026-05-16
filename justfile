set shell := ["bash", "-euo", "pipefail", "-c"]

repo_root := justfile_directory()
solution := repo_root + "/Dottie.slnx"
cli_project := repo_root + "/src/Dottie.Cli"
dottie_dir := env_var_or_default("DOTTIE_DIR", env_var("HOME") + "/.dottie")

default:
    @just --list

restore:
    dotnet restore {{solution}}

build:
    dotnet build {{solution}}

test:
    dotnet test {{solution}}

run *args:
    dotnet run --project {{cli_project}} -- {{args}}

local-validate *args:
    cd {{dottie_dir}} && dotnet run --project {{cli_project}} -- validate {{args}}

local-link *args:
    cd {{dottie_dir}} && dotnet run --project {{cli_project}} -- link {{args}}

local-install *args:
    cd {{dottie_dir}} && dotnet run --project {{cli_project}} -- install {{args}}

local-apply *args:
    cd {{dottie_dir}} && dotnet run --project {{cli_project}} -- apply {{args}}

local-status *args:
    cd {{dottie_dir}} && dotnet run --project {{cli_project}} -- status {{args}}
