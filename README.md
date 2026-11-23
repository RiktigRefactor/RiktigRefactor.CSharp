# RiktigRefactor.CSharp

[![Build and test](https://github.com/RiktigRefactor/riktigrefactor.csharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/RiktigRefactor/riktigrefactor.csharp/actions/workflows/build-and-test.yml)

Behavior-preserving refactorings for C#, built on Roslyn.

*Riktig* (Swedish/Norwegian) means correct.
When the tool cannot prove a transformation is safe, it refuses with a clear reason instead of guessing.

This is the C# implementation of the [RiktigRefactor specification](https://github.com/RiktigRefactor/riktigrefactor.spec).
The specification defines the correctness model, the refactorings, the JSON protocol, and the test format.

## What it can do

The tool has four refactorings: Rename, Extract IIFE, Make method static, and Move static member.

Rename renames a symbol and every reference to it across a whole solution or project.

After Roslyn computes the rename, the tool re-binds the result and checks that no reference changed which symbol it binds to.
It refuses (exit code 1) when it can't prove the rename preserves behavior.

Extract IIFE (also called extract static lambda) wraps a selected expression, or whole statements of one block, in an immediately-invoked `static` lambda:

```csharp
// Before
var total = price * qty;

// After
var total = ((Func<decimal, int, decimal>)(static (price, qty) => price * qty))(price, qty);
```

Variables from outside become parameters, and `this` becomes a parameter named `@this`.
Variables the statements assign and the code after them reads, or declare and the code after them uses, are returned in a tuple and assigned at the call site.
It refuses when it can't prove that wrapping keeps behavior.
After extracting, it checks that the result compiles and that every name binds as before.

Make method static turns an instance method into a static method on the same type, with the receiver as a new first parameter, and rewrites every call:

```csharp
// Before
public decimal Total(int qty) => price * qty;
var t = order.Total(3);

// After
public static decimal Total(Order order, int qty) => order.price * qty;
var t = Order.Total(order, 3);
```

A struct receiver is passed by `ref`, or by `in` when the method is `readonly` or the struct is a `readonly struct`.
It refuses when it can't prove that every call keeps its behavior.

Move static member moves a static method (including an extension method), field, property, event or constant to another class, struct or record in the same project, and updates every reference.
References written through the source type, a subclass, or `using static`, and unqualified ones inside the source, become `Target.Member`.
Names in the moved declaration that were found through the source's scope are qualified, such as `Source.Rate`.
It refuses when it can't prove that the move keeps behavior, including static initialization order.

Each refusal has a code, listed in [refusal-codes.md](refusal-codes.md).

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

Preview an Extract IIFE of the text from line 12, column 19 up to (not including) column 30:

```shell
riktig-cs extract-iife --workspace App.sln --file src/Foo.cs --from 12:19 --to 12:30
```

Make the method at line 5, column 20 static, naming its receiver parameter `self`:

```shell
riktig-cs make-method-static --workspace App.sln --file src/Order.cs --at 5:20 --receiver-name self
```

Move the static member at line 8, column 27 to `Shop.Pricing`:

```shell
riktig-cs move-static-member --workspace App.sln --file src/Order.cs --at 8:27 --target Shop.Pricing
```

The workspace must be restored (`dotnet restore`) so that types resolve; otherwise the tool refuses.

The exit code is 0 on success, 1 when the refactoring is refused, and 2 on an error such as an unknown file.

## Build and test

Run `./build-and-test` on Linux or macOS, or `build-and-test.cmd` on Windows (it needs Git for Windows).
It checks formatting and runs every test.
Run `./tidy-code` to format the code.

Test cases live in `RiktigRefactor.CSharp.Tests/Cases`, in the [format defined by the specification](https://github.com/RiktigRefactor/riktigrefactor.spec/blob/main/test-format.md).
Every input and expected program is compiled and run, and edge cases must fail under a deliberately naive implementation.

### Citing the C# specification

Every test case lists the language-specification sections it covers in its `spec` field, written as `C# spec §<number> <title>`, for example `C# spec §12.6.4 Overload resolution`.
The numbers and titles follow the C# standard draft published at [learn.microsoft.com](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/), which comes from the [csharpstandard](https://github.com/dotnet/csharpstandard) repository, at its `alpha-v13` branch, which is the one learn.microsoft.com currently renders from (see the `original_content_git_url` metadata on those pages).
Features that are not in the standard yet are cited as `C# feature: <name>`.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the commit message convention.
