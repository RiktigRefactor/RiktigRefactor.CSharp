using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Analysis;

public static class MoveGenerics
{
    /// <summary>True if the type or one of its containing types is generic, so each constructed type has its own static state and type parameters.</summary>
    public static bool IsGenericOrInGeneric(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }
}
