using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Analysis;

/// <summary>Helpers for refactorings that change a method and every call to it.</summary>
public static class MethodTargets
{
    /// <summary>Names the compiler or the runtime looks up by pattern, so changing such a method can change code that never names it.</summary>
    public static readonly IReadOnlySet<string> PatternNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "GetEnumerator", "GetAsyncEnumerator", "MoveNext", "MoveNextAsync", "Current", "Dispose", "DisposeAsync",
        "GetAwaiter", "GetResult", "IsCompleted", "OnCompleted", "UnsafeOnCompleted", "Deconstruct", "Add",
        "GetPinnableReference", "Slice", "Length", "Count", "Invoke", "Create",
        "Select", "SelectMany", "Where", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
        "GroupBy", "Join", "GroupJoin", "Cast",
        "ToString", "Equals", "GetHashCode", "Finalize", "MemberwiseClone", "GetType",
    };

    public static async Task<IMethodSymbol?> FindMethodAtAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
        return symbol is IMethodSymbol method ? (IMethodSymbol)SymbolIdentity.Normalize(method) : null;
    }

    /// <summary>Every source location that refers to <paramref name="method"/>, excluding its declaration.</summary>
    public static async Task<List<Location>> FindReferenceLocationsAsync(Solution solution, IMethodSymbol method, CancellationToken cancellationToken)
    {
        var references = await SymbolFinder.FindReferencesAsync(method, solution, cancellationToken).ConfigureAwait(false);
        return [.. references
            .Where(reference => SymbolEqualityComparer.Default.Equals(SymbolIdentity.Normalize(reference.Definition), method))
            .SelectMany(reference => reference.Locations)
            .Select(location => location.Location)
            .Where(location => location.IsInSource)
            .Distinct()];
    }

    /// <summary>True if <paramref name="method"/> implements an interface member, for its own type or any type in the solution that derives from it.</summary>
    public static async Task<bool> ImplementsInterfaceMemberAsync(Solution solution, IMethodSymbol method, CancellationToken cancellationToken)
    {
        var methodId = SymbolIdentity.Describe(solution, method);
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            foreach (var type in AllTypes(compilation!.Assembly.GlobalNamespace))
            {
                foreach (var member in type.AllInterfaces.SelectMany(face => face.GetMembers()))
                {
                    if (type.FindImplementationForInterfaceMember(member) is { } implementation
                        && SymbolIdentity.Describe(solution, implementation) == methodId)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            foreach (var nested in AllTypesAndNested(type))
            {
                yield return nested;
            }
        }

        foreach (var child in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in AllTypes(child))
            {
                yield return type;
            }
        }
    }

    /// <summary>The first conditionally compiled region that mentions <paramref name="name"/>, whose code the refactoring can't see.</summary>
    public static async Task<Location?> FindDisabledTextMentioningAsync(Solution solution, string name, CancellationToken cancellationToken)
    {
        foreach (var document in solution.Projects.SelectMany(project => project.Documents))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var disabled = root?.DescendantTrivia(descendIntoTrivia: true)
                .FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia) && MentionsWord(trivia.ToString(), name));
            if (disabled is { } trivia && trivia != default)
            {
                return trivia.GetLocation();
            }
        }

        return null;
    }

    /// <summary>The name of a camel-case local for a type named <paramref name="typeName"/>, escaped if it is a keyword.</summary>
    public static string LocalNameFor(string typeName)
    {
        var name = char.ToLowerInvariant(typeName[0]) + typeName[1..];
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }

    /// <summary>True if <paramref name="name"/> is a valid identifier; a keyword is valid only escaped with <c>@</c>.</summary>
    public static bool IsValidIdentifier(string name)
    {
        var unescaped = name.StartsWith('@') ? name[1..] : name;
        return SyntaxFacts.IsValidIdentifier(unescaped) && (name.StartsWith('@') || SyntaxFacts.GetKeywordKind(unescaped) == SyntaxKind.None);
    }

    static bool MentionsWord(string text, string word)
    {
        for (var index = text.IndexOf(word, StringComparison.Ordinal); index >= 0; index = text.IndexOf(word, index + 1, StringComparison.Ordinal))
        {
            var startsWord = index == 0 || !SyntaxFacts.IsIdentifierPartCharacter(text[index - 1]);
            var endsWord = index + word.Length == text.Length || !SyntaxFacts.IsIdentifierPartCharacter(text[index + word.Length]);
            if (startsWord && endsWord)
            {
                return true;
            }
        }

        return false;
    }

    static IEnumerable<INamedTypeSymbol> AllTypesAndNested(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers().SelectMany(AllTypesAndNested))
        {
            yield return nested;
        }
    }
}
