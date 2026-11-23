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
| `would-introduce-errors` | The rename would introduce compile errors that are not a declaration conflict. |
| `binding-would-change` | After the rename, a reference would bind to a different symbol, such as when a name is captured or an overload resolution changes. Conflict repairs that insert casts are refused with this code. |
| `dynamic-access-possible` | A dynamically bound member access or named argument uses the old or new name. |
| `string-names-symbol` | A string names the symbol in a way the rename would not update, such as a `[CallerArgumentExpression]` argument naming the parameter. |
| `preprocessor-region` | A conditionally compiled (`#if`) region mentions the old or new name. |
| `internal-verification-failed` | The tool's own check of its result failed. This is a bug in the tool; please report it. |
