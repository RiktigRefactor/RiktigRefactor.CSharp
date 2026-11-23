This repo implements the C# side of RiktigRefactor.
The specification is the source of truth: https://github.com/RiktigRefactor/riktigrefactor.spec
Read its `README.md`, `principles.md`, and the page for the refactoring you are changing before you change behavior.
Soundness comes before completeness: refusing is always acceptable, and wrong output never is.

Don't bother touching line endings and BOMs.
Let `./tidy-code` handle that as needed.

When asked to "build" or "test", run `./build-and-test`.
Also run it automatically before each commit.

## Test cases

Rename cases are directories under `RiktigRefactor.CSharp.Tests/Cases/Rename/` holding `input/` (and `expected/` unless a refusal is expected) as real `.cs` files.
Each case has one `[Fact]` in the matching `*RenameCaseTests.cs` class, which supplies the options, spec citations and expectations through `RenameCase`; there is no `case.json`.
Extract IIFE cases work the same way under `RiktigRefactor.CSharp.Tests/Cases/ExtractIife/`, with one `[Fact]` per case in a `*ExtractIifeCaseTests.cs` class using `ExtractIifeCase`, and the selection marked with `/*[*/` and `/*]*/`.
Make method static, Move static member and Move static method to instance method cases work the same way under `Cases/MakeMethodStatic/`, `Cases/MoveStaticMember/` and `Cases/MoveToInstanceMethod/`, with one `[Fact]` per case in the matching `*CaseTests.cs` class using its `*Case` record, and the member marked with the cursor `/*|*/`.
Refactorings other than Rename and Extract IIFE rewrite code through `Rewriting/RewrittenSolution`, which maps original text through the edits so `RewriteVerifier` can check every name, and share `CaseRunner` in the tests.
Every input and expected file set is a complete program that throws if its behavior is wrong.
To create the `expected/` directory for a new case, run the tests once with `RIKTIG_WRITE_EXPECTED=1`, then read the output and confirm it is correct and behavior-preserving before keeping it.
Edge cases call `.NaiveShouldFail()` so the tests prove the assertion can detect the problem.

## Secrets

Always obscure API keys and other secrets, especially in saved screenshots.

## C#

A `switch` should only ever have a single statement in a case.
If it's going to be more than that, extract.

Avoid ternary (`?:`) except for very simple cases.

Don't use `sealed` on classes or records.

The most important thing about a name is that it not be misleading.
A reader should never need to look at how a variable or method is used/implemented to understand what it represents.
It is OK to make the name long to achieve this.
If you see a comment explaining what a function or variable does/is, rename it to make it more obvious.

## Committing

Before committing, run `./build-and-test`.
If the change has more than one thing going on, split it into multiple commits, each with a clear purpose.
Especially separate refactoring from behavior changes.
See [CONTRIBUTING.md](CONTRIBUTING.md) for the commit notation convention.

## Cloud sessions

- `gh pr create` is blocked in cloud sessions (it uses GraphQL).
  Open pull requests with `gh api repos/OWNER/REPO/pulls -f title=... -f head=... -f base=main -f body=...`.
- Cloud sessions reach the network only through an allowlisting proxy.
  Hosts needed: NuGet (`api.nuget.org`, `*.nuget.org`), PyPI (`pypi.org`, `files.pythonhosted.org`), and LLVM (`apt.llvm.org`, `github.com` for LLVM release downloads).
- Cloud sessions need both the .NET 11 SDK and the .NET 10 runtime, because the tests target `net10.0`.

## After every task

At the end of every task report on all of these, briefly:
- Skills used: name them.
- Skill gaps: if a skill you wished existed would have helped, propose one, even briefly.
- Prompt feedback: one concrete way the request could have been made more effectively this time — skip it only if there's genuinely nothing worth saying.
- Questions, ideas, concerns: surface them as they come up, not just at the end.
- Changes to your skills and instructions that would reduce token usage or improve efficiency.

Treat this end-of-task report as mandatory, like a checklist item — don't let it get crowded out by concise-output-style guidance; it's a small fixed addition, not a violation of terseness.

Never use Claude Code's persistent memory system for this project.
Durable guidance (conventions, process corrections, preferences) belongs in-repo instead — this file or a skill — so it's visible in version control and to every contributor, not just stored privately for one user.

## Decisions

When you need input on a decision:
- First, pick the option you think is best and propose it. Start that message with `⭐ 1: `
- Also list the alternatives you rejected, each with a brief reason why. Start each with `❌ n:`

Then have a discussion.
This formatting applies every time you present a decision, even a quick one — don't let a short or informal-feeling exchange skip it.
