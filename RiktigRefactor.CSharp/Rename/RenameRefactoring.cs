using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rename;

public static class RenameRefactoring
{
    /// <summary>Compiler errors that mean the new name collides with an existing declaration.</summary>
    static readonly HashSet<string> DeclarationConflictErrors = ["CS0100", "CS0101", "CS0102", "CS0111", "CS0128", "CS0136"];

    public static async Task<RefactoringResult> RenameAsync(
        Solution solution,
        DocumentId documentId,
        int position,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var document = solution.GetDocument(documentId) ?? throw new ArgumentException("Document not in solution.", nameof(documentId));
        var symbol = await FindRenameableSymbolAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (symbol.Refusal is { } refusal)
        {
            return RefactoringResult.Refuse(refusal);
        }

        var target = symbol.Symbol!;
        var oldName = DeclaredName(target);
        var unescapedNewName = newName.StartsWith('@') ? newName[1..] : newName;
        var isReservedKeyword = SyntaxFacts.GetKeywordKind(unescapedNewName) != SyntaxKind.None;
        if (!SyntaxFacts.IsValidIdentifier(unescapedNewName) || (isReservedKeyword && !newName.StartsWith('@')))
        {
            return RefactoringResult.Refuse(
                RefusalCodes.InvalidIdentifier,
                $"`{newName}` is not a valid C# identifier.",
                suggestion: isReservedKeyword ? $"Write `@{newName}` to use a keyword as an identifier." : null);
        }

        if (unescapedNewName == oldName)
        {
            return new RefactoringResult(RefactoringStatus.Success, [], [], []) { NewSolution = solution };
        }

        var earlyRefusal = RenamePreconditions.FindExternalHierarchy(solution, target)
            ?? RenamePreconditions.FindParameterNamedInString(target, unescapedNewName)
            ?? await RenamePreconditions.FindDynamicAccessAsync(solution, oldName, unescapedNewName, cancellationToken).ConfigureAwait(false);
        if (earlyRefusal is not null)
        {
            return RefactoringResult.Refuse(earlyRefusal);
        }

        var preprocessorRefusal = await FindPreprocessorConflictAsync(solution, oldName, unescapedNewName, cancellationToken).ConfigureAwait(false)
            ?? await RenamePreconditions.FindPatternBasedUseAsync(solution, target, oldName, unescapedNewName, cancellationToken).ConfigureAwait(false)
            ?? await RenamePreconditions.FindGeneratedCodeUseAsync(solution, oldName, unescapedNewName, cancellationToken).ConfigureAwait(false);
        if (preprocessorRefusal is not null)
        {
            return RefactoringResult.Refuse(preprocessorRefusal);
        }

        var originalSolution = solution;
        if (await InferredAnonymousMemberNames.MakeExplicitAsync(solution, target, cancellationToken).ConfigureAwait(false) is not { } withExplicitNames)
        {
            return RefactoringResult.Refuse(
                RefusalCodes.InternalVerificationFailed,
                "Naming inferred anonymous type members explicitly changed the anonymous types.");
        }

        (solution, target) = withExplicitNames;
        if (await ImplicitDllImportEntryPoints.MakeExplicitAsync(solution, target, cancellationToken).ConfigureAwait(false) is not { } withExplicitEntryPoint)
        {
            return RefactoringResult.Refuse(
                RefusalCodes.InternalVerificationFailed,
                "Writing the DllImport entry point explicitly changed the entry point.");
        }

        (solution, target) = withExplicitEntryPoint;
        var errorsBefore = await ErrorCountsAsync(solution, cancellationToken).ConfigureAwait(false);
        var options = new SymbolRenameOptions(RenameOverloads: false, RenameInStrings: false, RenameInComments: false, RenameFile: false);
        var newSolution = await Renamer.RenameSymbolAsync(solution, target, options, unescapedNewName, cancellationToken).ConfigureAwait(false);
        newSolution = await CapturedStaticMemberReferences.QualifyAsync(solution, newSolution, target, newName, cancellationToken).ConfigureAwait(false);

        var errorsAfter = await ErrorCountsAsync(newSolution, cancellationToken).ConfigureAwait(false);
        var newErrors = errorsAfter.Where(pair => pair.Value > errorsBefore.GetValueOrDefault(pair.Key)).Select(pair => pair.Key).ToList();
        if (newErrors.Count > 0)
        {
            var isDeclarationConflict = newErrors.Any(error => DeclarationConflictErrors.Contains(error.Id));
            return RefactoringResult.Refuse(
                isDeclarationConflict ? RefusalCodes.ConflictingDeclaration : RefusalCodes.WouldIntroduceErrors,
                $"Renaming `{oldName}` to `{unescapedNewName}` would introduce compile errors ({string.Join(", ", newErrors.Select(error => error.Id).Distinct())}) in {string.Join(", ", newErrors.Select(error => error.File).Distinct())}.",
                suggestion: "Choose a different name.");
        }

        var generatedOutputChange = await RenamePreconditions.FindGeneratedOutputChangeAsync(solution, newSolution, cancellationToken).ConfigureAwait(false);
        if (generatedOutputChange is not null)
        {
            return RefactoringResult.Refuse(generatedOutputChange);
        }

        var renamedSymbols = await FindRenamedSymbolsAsync(solution, target, unescapedNewName, cancellationToken).ConfigureAwait(false);
        var bindingChange = await BindingVerifier.FindFirstBindingChangeAsync(solution, newSolution, renamedSymbols, cancellationToken).ConfigureAwait(false);
        if (bindingChange is not null)
        {
            return RefactoringResult.Refuse(bindingChange with { Suggestion = "Choose a different name." });
        }

        var fileRefusal = await FindDivergingLinkedFileAsync(newSolution, cancellationToken).ConfigureAwait(false)
            ?? await FindEditedGeneratedFileAsync(solution, newSolution, cancellationToken).ConfigureAwait(false);
        if (fileRefusal is not null)
        {
            return RefactoringResult.Refuse(fileRefusal);
        }

        var edits = await Edits.ComputeAsync(originalSolution, newSolution, cancellationToken).ConfigureAwait(false);
        var warnings = await FindStringsMatchingNameAsync(solution, oldName, cancellationToken).ConfigureAwait(false);
        return new RefactoringResult(RefactoringStatus.Success, edits, warnings, []) { NewSolution = newSolution };
    }

    static string DeclaredName(ISymbol symbol) => symbol switch
    {
        IMethodSymbol { ExplicitInterfaceImplementations: [var implemented, ..] } => implemented.Name,
        IPropertySymbol { ExplicitInterfaceImplementations: [var implemented, ..] } => implemented.Name,
        IEventSymbol { ExplicitInterfaceImplementations: [var implemented, ..] } => implemented.Name,
        _ => symbol.Name,
    };

    /// <summary>The target and the symbols that must be renamed with it (overrides, implementations, and the like), which share its name and are declared in source.</summary>
    static async Task<Dictionary<ISymbol, string>> FindRenamedSymbolsAsync(
        Solution solution,
        ISymbol target,
        string newName,
        CancellationToken cancellationToken)
    {
        var renamed = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default) { [target] = newName };
        foreach (var referenced in await SymbolFinder.FindReferencesAsync(target, solution, cancellationToken).ConfigureAwait(false))
        {
            var isCascade = DeclaredName(referenced.Definition) == DeclaredName(target) && referenced.Definition.Locations.Any(location => location.IsInSource);
            if (isCascade)
            {
                renamed[referenced.Definition] = newName;
            }
        }

        return renamed;
    }

    static async Task<(ISymbol? Symbol, RefusalReason? Refusal)> FindRenameableSymbolAsync(
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (symbol is null)
        {
            return (null, new RefusalReason(RefusalCodes.NoSymbol, "There is no renameable symbol at the cursor.", null, "Put the cursor on a name."));
        }

        symbol = SymbolIdentity.Normalize(symbol);
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.Destructor } method)
        {
            symbol = method.ContainingType;
        }

        var isUnsupportedMethod = symbol is IMethodSymbol
        {
            MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion or MethodKind.BuiltinOperator
        };
        var isUnsupported = isUnsupportedMethod
            || symbol is IPropertySymbol { IsIndexer: true }
            || symbol.IsImplicitlyDeclared
            || symbol is INamespaceSymbol { IsGlobalNamespace: true }
            || symbol is IAliasSymbol { Name: "global" };
        if (isUnsupported)
        {
            return (null, new RefusalReason(RefusalCodes.NotRenameable, $"`{symbol.ToDisplayString()}` can't be renamed.", null, null));
        }

        if (symbol.Locations.Any(location => !location.IsInSource || document.Project.Solution.GetDocumentId(location.SourceTree) is null))
        {
            return (null, new RefusalReason(
                RefusalCodes.NotRenameable,
                $"`{symbol.ToDisplayString()}` is declared outside this workspace.",
                null,
                null));
        }

        return (symbol, null);
    }

    /// <summary>Refuses when copies of a file linked into several projects would end up with different text, which can't be written back.</summary>
    static async Task<RefusalReason?> FindDivergingLinkedFileAsync(Solution newSolution, CancellationToken cancellationToken)
    {
        var documentsByPath = newSolution.Projects.SelectMany(project => project.Documents)
            .Where(document => document.FilePath is not null)
            .GroupBy(document => document.FilePath!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);
        foreach (var linked in documentsByPath)
        {
            var texts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var document in linked)
            {
                texts.Add((await document.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString());
            }

            if (texts.Count > 1)
            {
                return new RefusalReason(
                    RefusalCodes.BindingWouldChange,
                    $"`{linked.Key}` is shared by several projects, and the rename would change it differently in each.",
                    null,
                    null);
            }
        }

        return null;
    }

    /// <summary>
    /// Refuses to edit a file that a tool generated (by the conventions Roslyn uses to recognize generated code),
    /// because regenerating it would undo the rename.
    /// </summary>
    static async Task<RefusalReason?> FindEditedGeneratedFileAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        foreach (var documentId in newSolution.GetChanges(oldSolution).GetProjectChanges()
            .SelectMany(changes => changes.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true)))
        {
            var document = oldSolution.GetDocument(documentId)!;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (IsGeneratedFileName(document.FilePath ?? document.Name) || HasAutoGeneratedHeader(root))
            {
                return new RefusalReason(
                    RefusalCodes.NotRenameable,
                    $"The rename would edit `{document.FilePath ?? document.Name}`, which is generated code and would be overwritten.",
                    null,
                    null);
            }
        }

        return null;
    }

    static bool IsGeneratedFileName(string path)
    {
        var fileName = Path.GetFileName(path);
        string[] generatedSuffixes = [".designer.cs", ".generated.cs", ".g.cs", ".g.i.cs"];
        return fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
            || generatedSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    static bool HasAutoGeneratedHeader(SyntaxNode? root)
        => root?.GetLeadingTrivia().Any(trivia =>
            (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            && (trivia.ToString().Contains("<auto-generated", StringComparison.OrdinalIgnoreCase)
                || trivia.ToString().Contains("<autogenerated", StringComparison.OrdinalIgnoreCase))) ?? false;

    static async Task<RefusalReason?> FindPreprocessorConflictAsync(
        Solution solution,
        string oldName,
        string newName,
        CancellationToken cancellationToken)
    {
        var names = new Regex($@"\b({Regex.Escape(oldName)}|{Regex.Escape(newName)})\b", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        foreach (var document in solution.Projects.SelectMany(project => project.Documents))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var disabled = root?.DescendantTrivia(descendIntoTrivia: true)
                .FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia) && names.IsMatch(trivia.ToString()));
            if (disabled is { } trivia && trivia != default)
            {
                return new RefusalReason(
                    RefusalCodes.PreprocessorRegion,
                    "A conditionally-compiled region mentions the old or new name, so the rename can't be verified in every configuration.",
                    Ranges.FromSpan(trivia.SyntaxTree!, trivia.Span),
                    null);
            }
        }

        return null;
    }

    static async Task<Dictionary<(string File, string Id), int>> ErrorCountsAsync(Solution solution, CancellationToken cancellationToken)
    {
        var counts = new Dictionary<(string File, string Id), int>();
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
            {
                continue;
            }

            foreach (var error in compilation.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                var key = (error.Location.SourceTree?.FilePath ?? project.Name, error.Id);
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
        }

        return counts;
    }

    static async Task<List<Warning>> FindStringsMatchingNameAsync(Solution solution, string oldName, CancellationToken cancellationToken)
    {
        var warnings = new List<Warning>();
        foreach (var document in solution.Projects.SelectMany(project => project.Documents))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var matches = root?.DescendantTokens()
                .Where(token => token.IsKind(SyntaxKind.StringLiteralToken) && token.ValueText == oldName) ?? [];
            warnings.AddRange(matches.Select(token => new Warning(
                WarningCodes.StringMatchesOldName,
                $"A string equals the old name `{oldName}`. If it's used for reflection or serialization, it wasn't updated.",
                Ranges.FromSpan(token.SyntaxTree!, token.Span))));
        }

        return warnings;
    }
}
