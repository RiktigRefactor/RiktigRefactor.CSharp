using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RiktigRefactor.CSharp.Rename;

/// <summary>
/// Decides whether a language construct that looks a member up by name (a collection initializer's <c>Add</c>, foreach's <c>GetEnumerator</c>, and the like)
/// could find the renamed member, under its old or new name.
/// Member lookup on a type searches only that type and its base types, so a construct whose lookup type is unrelated to every type declaring a renamed member can't be affected.
/// Whenever the lookup type isn't known exactly, the answer is yes.
/// </summary>
public static class PatternLookupScope
{
    static readonly HashSet<string> EnumeratorNames = new(StringComparer.Ordinal) { "GetEnumerator", "MoveNext", "Current", "Dispose" };
    static readonly HashSet<string> AwaiterNames = new(StringComparer.Ordinal) { "GetAwaiter", "GetResult", "IsCompleted", "OnCompleted", "UnsafeOnCompleted" };

    /// <summary>The types declaring the renamed member and the members renamed with it, or null if any of them can be found by lookups on unrelated types (extension members).</summary>
    public static IReadOnlyList<INamedTypeSymbol>? ContainingTypesOfRenamedMembers(ISymbol symbol, IEnumerable<ISymbol> relatedMembers)
    {
        var members = relatedMembers.Prepend(symbol).ToList();
        var isExtension = members.Any(member => member is IMethodSymbol { IsExtensionMethod: true } || member.ContainingType is null || member.ContainingType.IsExtension);
        return isExtension ? null : [.. members.Select(member => member.ContainingType!.OriginalDefinition).Distinct(SymbolEqualityComparer.Default).Cast<INamedTypeSymbol>()];
    }

    public static bool MayFind(SemanticModel model, SyntaxNode construct, string name, IReadOnlyList<INamedTypeSymbol>? renamedMemberTypes, CancellationToken cancellationToken)
    {
        if (renamedMemberTypes is null)
        {
            return true;
        }

        var lookupTypes = LookupTypes(model, construct, name, cancellationToken);
        return lookupTypes is null || lookupTypes.Any(type => MayContainMembersOf(type, renamedMemberTypes));
    }

    /// <summary>The types the construct looks <paramref name="name"/> up on, or null when they can't be determined exactly.</summary>
    static IReadOnlyList<ITypeSymbol?>? LookupTypes(SemanticModel model, SyntaxNode construct, string name, CancellationToken cancellationToken) => construct switch
    {
        InitializerExpressionSyntax { Parent: BaseObjectCreationExpressionSyntax creation } when name == "Add"
            => [model.GetTypeInfo(creation, cancellationToken).Type],
        ForEachStatementSyntax { AwaitKeyword.RawKind: 0 } forEach when EnumeratorNames.Contains(name)
            => ForEachTypes(model, forEach, cancellationToken),
        AwaitExpressionSyntax awaitExpression when AwaiterNames.Contains(name)
            => AwaitTypes(model, awaitExpression, cancellationToken),
        UsingStatementSyntax { AwaitKeyword.RawKind: 0 } usingStatement when name == "Dispose"
            => [UsingResourceType(model, usingStatement, cancellationToken)],
        RecordDeclarationSyntax record => [model.GetDeclaredSymbol(record, cancellationToken)],
        _ => null,
    };

    static IReadOnlyList<ITypeSymbol?>? ForEachTypes(SemanticModel model, ForEachStatementSyntax forEach, CancellationToken cancellationToken)
    {
        var getEnumerator = model.GetForEachStatementInfo(forEach).GetEnumeratorMethod;
        return getEnumerator is null ? null : [model.GetTypeInfo(forEach.Expression, cancellationToken).Type, getEnumerator.ReturnType];
    }

    static IReadOnlyList<ITypeSymbol?>? AwaitTypes(SemanticModel model, AwaitExpressionSyntax awaitExpression, CancellationToken cancellationToken)
    {
        var getAwaiter = model.GetAwaitExpressionInfo(awaitExpression).GetAwaiterMethod;
        return getAwaiter is null ? null : [model.GetTypeInfo(awaitExpression.Expression, cancellationToken).Type, getAwaiter.ReturnType];
    }

    static ITypeSymbol? UsingResourceType(SemanticModel model, UsingStatementSyntax usingStatement, CancellationToken cancellationToken)
    {
        if (usingStatement.Expression is { } expression)
        {
            return model.GetTypeInfo(expression, cancellationToken).Type;
        }

        return usingStatement.Declaration is { } declaration ? model.GetTypeInfo(declaration.Type, cancellationToken).Type : null;
    }

    /// <summary>True unless lookup on <paramref name="type"/> certainly can't find a member of any of <paramref name="declaringTypes"/>.</summary>
    static bool MayContainMembersOf(ITypeSymbol? type, IReadOnlyList<INamedTypeSymbol> declaringTypes)
    {
        if (type is null || type.TypeKind is TypeKind.Dynamic or TypeKind.Error or TypeKind.TypeParameter or TypeKind.Pointer or TypeKind.FunctionPointer)
        {
            return true;
        }

        var searched = new List<ITypeSymbol>();
        for (var current = type; current is not null; current = current.BaseType)
        {
            searched.Add(current.OriginalDefinition);
        }

        searched.AddRange(type.AllInterfaces.Select(@interface => @interface.OriginalDefinition));
        return searched.Any(candidate => declaringTypes.Any(declaring => SymbolEqualityComparer.Default.Equals(candidate, declaring)));
    }
}
