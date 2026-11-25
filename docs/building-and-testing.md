# Building and testing

Install [Git](https://git-scm.com/) and [Mise](https://mise.jdx.dev/).
Mise provides the .NET SDK pinned in `global.json` and the .NET 10 runtime the projects target, so nothing else needs to be installed:

```shell
./build-and-test
```

`build-and-test` builds and runs every test case; `build-and-test.cmd` does the same on Windows through Git for Windows' Bash.
Run `./tidy-code` to format the code.
CI formats the code and updates the Markdown snippets in separate workflows.

Test cases live in `RiktigRefactor.CSharp.Tests/Cases`, in the [format defined by the specification](https://github.com/RiktigRefactor/riktigrefactor.spec/blob/main/test-format.md).
Every input and expected program is compiled and run, and edge cases must fail under a deliberately naive rename.

## Publishing

Every push to `main` publishes a prerelease such as `0.1.1-ci.9`.
To release, push a tag:

```shell
git tag v0.2.0
git push origin v0.2.0
```

[MinVer](https://github.com/adamralph/minver) computes the version from the tags.
Both cases use `.github/workflows/_publish.yml`, which publishes to nuget.org.

Publishing uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), so there is no stored API key.
It needs a trusted publishing policy on nuget.org, and a `NUGET_USER` repository variable naming the nuget.org profile that owns the policy.

### Citing the C# specification

Every test case lists the language-specification sections it covers in its `spec` field, written as `C# spec §<number> <title>`, for example `C# spec §12.6.4 Overload resolution`.
The numbers and titles follow the C# standard draft published at [learn.microsoft.com](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/), which comes from the [csharpstandard](https://github.com/dotnet/csharpstandard) repository, at its `alpha-v13` branch, which is the one learn.microsoft.com currently renders from (see the `original_content_git_url` metadata on those pages).
Features that are not in the standard yet are cited as `C# feature: <name>`.
