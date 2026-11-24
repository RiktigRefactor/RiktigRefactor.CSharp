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
