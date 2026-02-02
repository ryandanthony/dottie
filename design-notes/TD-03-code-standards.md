# Tech Debt 03 - Enable Code Standards

**STATUS**: Done

At some point `FestinaLente.CodeStandards` was disabled in `Directory.Build.props`
Create a new branch to address this issue:

1) Reenable FestinaLente.CodeStandards & enable `Treat Warnings as errors` too.
2) Check changes in, create a PR to check if the CI/Github Actions will work properly.
    1) Fix any issues with github actions
3) fix all of the invalid code because it isnt up to standards
    1) try using `dotnet format` to fix the easy automatic stuff
    2) one by one address each error type
4) validate taht all unit tests pass
5) validate taht all integration tests pass
6) validate that the CI unit tests are run 
