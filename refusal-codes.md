# Refusal codes

The C# tool defines its own refusal codes; the [specification](https://github.com/RiktigRefactor/riktigrefactor.spec) does not list any, except `internal-verification-failed`.
This file is the source of truth for them, and `RiktigRefactor.CSharp/Protocol/RefusalCodes.cs` defines them in code.
Codes are stable, and messages may change.
A new code is listed in the release notes.

| Code | Meaning |
|---|---|
| `no-symbol` | There is no renameable symbol, or no method or member, at the cursor. |
| `not-renameable` | The symbol can't be renamed: it is not declared in the workspace, it overrides or implements a member that is not, or the rename would edit generated code. |
| `invalid-identifier` | The new name (or a new parameter's name) is not a valid identifier, or is a keyword without an `@` escape. |
| `conflicting-declaration` | The new name, or a method's new signature, collides with an existing declaration. |
| `would-introduce-errors` | The change would introduce compile errors that are not a declaration conflict. |
| `binding-would-change` | After the change, a reference would bind to a different symbol, such as when a name is captured or an overload resolution changes. Conflict repairs that insert casts are refused with this code. |
| `dynamic-access-possible` | A dynamically bound member access or named argument uses the old or new name. |
| `string-names-symbol` | A string names the symbol in a way the rename would not update, such as a `[CallerArgumentExpression]` argument naming the parameter. |
| `preprocessor-region` | A conditionally compiled (`#if`) region mentions the old or new name (or the method being changed), or the Extract IIFE selection contains a preprocessor directive. |
| `invalid-selection` | Extract IIFE: the selection is not exactly one expression or whole statements of one block, or does not denote a value. |
| `non-local-control-flow` | Extract IIFE: `return`, `break`, `continue`, `goto`, `yield`, or a label would cross the boundary of the lambda. |
| `contains-await` | Extract IIFE: the selection contains `await` (outside a nested lambda or local function). |
| `type-not-nameable` | Extract IIFE: a type that would have to be written as a type argument can't be, such as an anonymous type. |
| `target-typed-expression` | Extract IIFE: the expression's meaning depends on its target type (such as `null`, a lambda, an interpolated string converted to `FormattableString`, or a constant converted implicitly), so returning it from a lambda could change it. |
| `selection-is-variable` | Extract IIFE: the expression denotes a variable, and the lambda would return a copy of its value. |
| `expression-writes-variable` | Extract IIFE: the selected expression assigns a local or parameter, which the lambda would change only in its copy. |
| `ref-or-pointer-across-boundary` | Extract IIFE: a `ref` local or parameter, a `ref struct`, a pointer, `stackalloc`, or `fixed` would cross the boundary of the lambda. |
| `base-access` | Extract IIFE: the selection uses `base`. Make method static: the method uses `base`. |
| `struct-this` | Extract IIFE: the selection uses `this` in a struct, which a lambda parameter would copy. |
| `this-not-rewritable` | Extract IIFE: the selection uses `this` in a way that can't be written as `@this` (such as the `field` keyword). |
| `struct-mutation-possible` | Extract IIFE: the selection may mutate a struct variable without Roslyn counting it as a write, so the lambda would mutate only its copy. |
| `captured-variable` | Extract IIFE: a variable crossing the boundary is captured by a lambda or local function, or is a primary constructor parameter, so code would see a stale copy. |
| `writes-lost-on-exception` | Extract IIFE: inside a `try`, the selection assigns a variable read afterwards; an exception would lose the assignment, because the lambda returns values only when it completes. |
| `declaration-escapes` | Extract IIFE: a variable the selection declares is used after it but can't be returned. |
| `disposal-would-move` | Extract IIFE: a `using` declaration would be disposed when the lambda returns instead of at the end of its block. |
| `expression-tree-or-query` | Extract IIFE: the selection is inside a query expression or an expression tree lambda. Make method static, Move static method to instance method: a call is inside one, whose shape would change. |
| `too-many-variables` | Extract IIFE: the lambda would need more than 16 parameters. |
| `not-instance-method` | Make method static: the method is not an ordinary instance method with a body, declared once (not `partial` or `extern`) in a class, struct or record of the workspace. |
| `dynamic-dispatch` | Make method static: the method is `virtual`, `abstract`, an `override`, or implements an interface member (for its own type or a derived type). |
| `implicit-reference` | Make method static, Move static member, Move static method to instance method: the method has a name the compiler looks up by pattern (such as `GetEnumerator`, `Add`, `Deconstruct`, or a LINQ query method), or is called implicitly. |
| `method-group-reference` | Make method static: the method is used as a method group, which binds its receiver when the delegate is created. Move static method to instance method: the method is used as a method group, and an instance method has no group that takes the receiver as an argument. |
| `receiver-may-be-null` | Make method static: a call's receiver can't be proven non-null, or the call is null-conditional. A null receiver throws before the body runs, and a static method would run the body. Move static method to instance method: the argument that would become the receiver can't be proven non-null; the suggestion is an extension method, which keeps the null behavior. |
| `receiver-not-variable` | Make method static: a struct receiver passed by `ref` is not a mutable variable (a `readonly` field, an `in` parameter, a temporary, or `this` in a readonly member), so the original call ran on a copy. |
| `unsupported-receiver` | Make method static: a call's receiver has a type that would need a conversion to the new parameter, such as a type parameter. Move static method to instance method: the receiver parameter is a struct, or an argument for it needs a conversion. |
| `not-movable` | Move static member: the member is not a static method, field, property, event or constant with its own declaration in a class, struct or record; or it is `partial`, `extern`, `abstract`, `virtual`, an `override`, or `Main`; or a reference to it is not written through a type name. |
| `invalid-target` | Move static member: the target is not a class, struct or record with a body in the same project, is the member's own type, or is not a top-level static class for an extension method. |
| `generic-type` | Move static member, Move static method to instance method: the member's type or the target is generic, or nested in a generic type. |
| `name-collision` | Move static member: the target already has a member with the name (other than method overloads), or the member would hide one of a base type of the target. Move static method to instance method: a type in the receiver type's hierarchy (base types or subclasses) has a member with the method's name. |
| `initialization-order` | Move static member: the member's initializer is not a constant, or the source or target has a static constructor or a static initializer that is not a constant, so static initialization would run at a different time. |
| `lock-on-type` | Move static member: the member locks on its type object, with `[MethodImpl(MethodImplOptions.Synchronized)]` or `lock (typeof(...))`. |
| `inaccessible` | Move static member, Move static method to instance method: a reference could not access the member in the target, or the member's body could not access something from the target, with the current accessibility. |
| `not-static-method` | Move static method to instance method: the method is not a non-generic static method with a body declared once in a type of the workspace, or is an extension method. |
| `invalid-receiver` | Move static method to instance method: the method has no parameter with that name; it is `ref`, `out`, `in`, `params`, `scoped` or optional; its type is not a class declared in the same project; or a call does not pass it as an argument of its own. |
| `receiver-written` | Move static method to instance method: the body assigns the receiver parameter, passes it by reference, or names it with `nameof`, which `this` can't be. |
| `evaluation-order` | Move static method to instance method: an argument written before the receiver can't be proven independent of it, and the receiver would be evaluated first. |
| `internal-verification-failed` | The tool's own check of its result failed. This is a bug in the tool; please report it. |
