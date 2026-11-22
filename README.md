# RiktigRefactor.CSharp

[![Build and test](https://github.com/RiktigRefactor/riktigrefactor.csharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/RiktigRefactor/riktigrefactor.csharp/actions/workflows/build-and-test.yml)

Behavior-preserving refactorings for C#, built on Roslyn.

*Riktig* (Swedish/Norwegian) means correct.
When the tool cannot prove a transformation is safe, it refuses with a clear reason instead of guessing.

This is the C# implementation of the [RiktigRefactor specification](https://github.com/RiktigRefactor/riktigrefactor.spec).
The specification defines the correctness model, the refactorings, the JSON protocol, and the test format.

## What it can do

Rename is the first refactoring.
It renames a symbol and every reference to it across a whole solution or project.

After Roslyn computes the rename, the tool re-binds the result and checks that no reference changed which symbol it binds to.
It refuses (exit code 1) when:

- the new name is not a valid identifier, or is a keyword without an `@` escape;
- the symbol is not declared in the workspace, or overrides or implements a member that is not;
- the rename would introduce a compile error, or collide with an existing declaration;
- a name would be captured, or an overload resolution would change, in a way that is not fixed by qualifying the name;
- a dynamically bound member access uses the old or new name;
- a conditionally compiled (`#if`) region mentions the old or new name.

See [limitations](https://github.com/RiktigRefactor/riktigrefactor.spec/blob/main/limitations.md) for what is out of scope, such as reflection, serialization by name, and updating comments and strings.
Rename warns when a string literal equals the old name.

## Command line

Install the tool:

```shell
dotnet tool install --global RiktigRefactor.CSharp.Tool
```

The package is not published yet.
Until then, run it from a clone with `dotnet run --project RiktigRefactor.CSharp.Tool --`.

Preview a rename as a diff:

```shell
riktig-cs rename --workspace App.sln --file src/Foo.cs --at 12:5 --new-name count
```

Positions are 1-based, and columns count Unicode scalar values.
`--workspace` accepts a `.sln`, `.slnx`, or `.csproj`.

- `--apply` writes the changes to disk.
- `--json` prints the result using the shared protocol, without the diff.

The exit code is 0 on success, 1 when the rename is refused, and 2 on an error such as an unknown file.

## Build and test

Run `./build-and-test` on Linux or macOS, or `build-and-test.cmd` on Windows (it needs Git for Windows).
It checks formatting and runs every test.
Run `./tidy-code` to format the code.

Test cases live in `RiktigRefactor.CSharp.Tests/Cases`, in the [format defined by the specification](https://github.com/RiktigRefactor/riktigrefactor.spec/blob/main/test-format.md).
Every input and expected program is compiled and run, and edge cases must fail under a deliberately naive rename.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the commit message convention.
