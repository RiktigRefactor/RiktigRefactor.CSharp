# Command line

Install the tool:

```shell
dotnet tool install --global RiktigRefactor.CSharp.Tool
```

The package is not published yet.
Until then, run it from a clone with `dotnet run --project RiktigRefactor.CSharp.Tool --`.

Positions are 1-based, and columns count Unicode scalar values.
`--workspace` accepts a `.sln`, `.slnx`, or `.csproj`.

- `--apply` writes the changes to disk.
- `--json` prints the result using the shared protocol, without the diff.

The exit code is 0 on success, 1 when the refactoring is refused, and 2 on an error such as an unknown file.

## Language server

```shell
riktig-cs lsp
```

Runs a language server on standard input and output, for editors such as the [VS Code extension](https://github.com/RiktigRefactor/RiktigRefactor.VSCode).
It offers Rename as a code action, and runs it when the client sends `workspace/executeCommand` with `riktig.refactor` and a protocol request.
It finds the `.sln`, `.slnx` or `.csproj` in the workspace folder, or takes `initializationOptions.workspace`.
It reads files from disk and reloads the workspace for every request, so the client must save files first.
The mapping is defined in the [specification](https://github.com/RiktigRefactor/riktigrefactor.spec/blob/main/interfaces.md#lsp-mapping).
