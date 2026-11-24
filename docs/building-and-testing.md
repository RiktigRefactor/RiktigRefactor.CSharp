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

### Citing the C# specification

Every test case lists the language-specification sections it covers in its `spec` field, written as `C# spec §<number> <title>`, for example `C# spec §12.6.4 Overload resolution`.
The numbers and titles follow the C# standard draft published at [learn.microsoft.com](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/), which comes from the [csharpstandard](https://github.com/dotnet/csharpstandard) repository, at its `alpha-v13` branch, which is the one learn.microsoft.com currently renders from (see the `original_content_git_url` metadata on those pages).
Features that are not in the standard yet are cited as `C# feature: <name>`.
