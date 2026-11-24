# RiktigRefactor.CSharp

[![Build and test](https://github.com/RiktigRefactor/RiktigRefactor.CSharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/RiktigRefactor/RiktigRefactor.CSharp/actions/workflows/build-and-test.yml)
![Status: early development](https://img.shields.io/badge/status-early%20development-orange)

Seriously safe behavior-preserving refactorings for C#.
*Riktig* means "correct" in Swedish/Norwegian.
When the tool cannot prove a transformation is safe, it refuses with a clear reason instead of guessing.

This is the C# implementation of the [RiktigRefactor specification](https://github.com/RiktigRefactor/riktigrefactor.spec).

<!-- toc -->
## Contents

  * [What it can do](#what-it-can-do)
    * [Rename](#rename)
    * [Extract IIFE](#extract-iife)
    * [Make method static](#make-method-static)
    * [Move static member](#move-static-member)
    * [Move static method to instance method](#move-static-method-to-instance-method)
  * [Using it](#using-it)
  * [Building and testing](#building-and-testing)<!-- endToc -->

## What it can do

The tool has five refactorings: Rename, Extract IIFE, Make method static, Move static member, and Move static method to instance method.

### Rename

Rename renames a symbol and every reference to it across a whole solution or project.

#### Before

```csharp
class Order
{
    int qty;
    int Total() => qty * 2;
}
```

#### After

```csharp
class Order
{
    int quantity;
    int Total() => quantity * 2;
}
```

See more details in [docs/rename.md](docs/rename.md).

### Extract IIFE

Extract IIFE (also called extract static lambda) wraps a selected expression, or whole statements of one block, in an immediately-invoked `static` lambda.

See more details in [docs/extract-iife.md](docs/extract-iife.md).

### Make method static

Make method static turns an instance method into a static method on the same type, with the receiver as a new first parameter, and rewrites every call.

See more details in [docs/make-method-static.md](docs/make-method-static.md).

### Move static member

Move static member moves a static method (including an extension method), field, property, event or constant to another class, struct or record in the same project, and updates every reference.

See more details in [docs/move-static-member.md](docs/move-static-member.md).

### Move static method to instance method

Move static method to instance method is the inverse of Make method static: a parameter of a class type becomes the receiver, the method moves into that class, and every call passes the argument as the receiver.

See more details in [docs/move-to-instance-method.md](docs/move-to-instance-method.md).

## Using it

The package is not published yet; run it from a clone:

```shell
dotnet run --project RiktigRefactor.CSharp.Tool -- rename --workspace App.sln --file src/Foo.cs --at 12:5 --new-name count
```

See [the command line](docs/command-line.md) for the options, exit codes and refusal codes.

## Building and testing

```shell
./build-and-test
```

For more information, see [CONTRIBUTING.md](CONTRIBUTING.md).