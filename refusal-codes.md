# Refusal codes

The C# tool defines its own refusal codes; the [specification](https://github.com/RiktigRefactor/riktigrefactor.spec) does not list any, except `internal-verification-failed`.
This file is the source of truth for them, and `RiktigRefactor.CSharp/Protocol/RefusalCodes.cs` defines them in code.
Codes are stable, and messages may change.
A new code is listed in the release notes.

| Code | Meaning |
|---|---|
| `no-symbol` | There is no renameable symbol at the cursor. |
| `not-renameable` | The symbol can't be renamed: it is not declared in the workspace, it overrides or implements a member that is not, or the rename would edit generated code. |
| `invalid-identifier` | The new name is not a valid identifier, or is a keyword without an `@` escape. |
| `conflicting-declaration` | The new name collides with an existing declaration. |
| `would-introduce-errors` | The change would introduce compile errors that are not a declaration conflict. |
| `binding-would-change` | After the rename, a reference would bind to a different symbol, such as when a name is captured or an overload resolution changes. Conflict repairs that insert casts are refused with this code. |
| `dynamic-access-possible` | A dynamically bound member access or named argument uses the old or new name. |
| `string-names-symbol` | A string names the symbol in a way the rename would not update, such as a `[CallerArgumentExpression]` argument naming the parameter. |
| `preprocessor-region` | A conditionally compiled (`#if`) region mentions the old or new name, or the Extract IIFE selection contains a preprocessor directive. |
| `invalid-selection` | Extract IIFE: the selection is not exactly one expression or whole statements of one block, or does not denote a value. |
| `non-local-control-flow` | Extract IIFE: `return`, `break`, `continue`, `goto`, `yield`, or a label would cross the boundary of the lambda. |
| `contains-await` | Extract IIFE: the selection contains `await` (outside a nested lambda or local function). |
| `type-not-nameable` | Extract IIFE: a type that would have to be written as a type argument can't be, such as an anonymous type. |
| `target-typed-expression` | Extract IIFE: the expression's meaning depends on its target type (such as `null`, a lambda, an interpolated string converted to `FormattableString`, or a constant converted implicitly), so returning it from a lambda could change it. |
| `selection-is-variable` | Extract IIFE: the expression denotes a variable, and the lambda would return a copy of its value. |
| `expression-writes-variable` | Extract IIFE: the selected expression assigns a local or parameter, which the lambda would change only in its copy. |
| `ref-or-pointer-across-boundary` | Extract IIFE: a `ref` local or parameter, a `ref struct`, a pointer, `stackalloc`, or `fixed` would cross the boundary of the lambda. |
| `base-access` | Extract IIFE: the selection uses `base`. |
| `struct-this` | Extract IIFE: the selection uses `this` in a struct, which a lambda parameter would copy. |
| `this-not-rewritable` | Extract IIFE: the selection uses `this` in a way that can't be written as `@this` (such as the `field` keyword). |
| `struct-mutation-possible` | Extract IIFE: the selection may mutate a struct variable without Roslyn counting it as a write, so the lambda would mutate only its copy. |
| `captured-variable` | Extract IIFE: a variable crossing the boundary is captured by a lambda or local function, or is a primary constructor parameter, so code would see a stale copy. |
| `writes-lost-on-exception` | Extract IIFE: inside a `try`, the selection assigns a variable read afterwards; an exception would lose the assignment, because the lambda returns values only when it completes. |
| `declaration-escapes` | Extract IIFE: a variable the selection declares is used after it but can't be returned. |
| `disposal-would-move` | Extract IIFE: a `using` declaration would be disposed when the lambda returns instead of at the end of its block. |
| `expression-tree-or-query` | Extract IIFE: the selection is inside a query expression or an expression tree lambda. |
| `too-many-variables` | Extract IIFE: the lambda would need more than 16 parameters. |
| `internal-verification-failed` | The tool's own check of its result failed. This is a bug in the tool; please report it. |
