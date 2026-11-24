# Rename

It renames a symbol and every reference to it across a whole solution or project.

After Roslyn computes the rename, the tool re-binds the result and checks that no reference changed which symbol it binds to.
It refuses (exit code 1) when:

- the new name is not a valid identifier, or is a keyword without an `@` escape;
- the symbol is not declared in the workspace, or overrides or implements a member that is not;
- the rename would introduce a compile error, or collide with an existing declaration;
- a name would be captured, or an overload resolution would change, in a way that is not fixed by qualifying the name;
- a dynamically bound member access uses the old or new name;
- a conditionally compiled (`#if`) region mentions the old or new name.

Each refusal has a code, listed in [refusal-codes.md](../refusal-codes.md).

See [limitations](https://github.com/RiktigRefactor/riktigrefactor.spec/blob/main/limitations.md) for what is out of scope, such as reflection, serialization by name, and updating comments and strings.
Rename warns when a string literal equals the old name.

## Command

Rename the symbol at line 12, column 5 to `count`:

```shell
riktig-cs rename --workspace App.sln --file src/Foo.cs --at 12:5 --new-name count
```

See [the command line](command-line.md) for the options every command accepts.
