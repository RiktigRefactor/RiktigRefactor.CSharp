using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.ExtractIife;

public static class IifeWriter
{
    const string ThisParameterName = "@this";
    const string IndentStep = "    ";

    static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <param name="expressionTypeFallback">The type to return when the selected expression has none; only the naive implementation passes one.</param>
    /// <returns>Null when the delegate would need more parameters than <c>Func</c> and <c>Action</c> allow.</returns>
    public static IifeText? Write(SemanticModel model, SourceText text, IifePlan plan, ITypeSymbol? expressionTypeFallback = null)
    {
        var selection = plan.Selection;
        var position = selection.Span.Start;
        var compilation = model.Compilation;

        var parameterTypes = new List<ITypeSymbol>();
        var parameterNames = new List<string>();
        var arguments = new List<string>();
        if (plan.InstanceType is { } instanceType)
        {
            parameterTypes.Add(instanceType);
            parameterNames.Add(ThisParameterName);
            arguments.Add("this");
        }

        foreach (var variable in plan.VariablesPassedIn)
        {
            parameterTypes.Add(plan.ParameterTypes.GetValueOrDefault(variable) ?? IifePlan.TypeOf(variable));
            parameterNames.Add(Escape(variable.Name));
            arguments.Add(Escape(variable.Name));
        }

        string Display(ITypeSymbol type) => type.ToMinimalDisplayString(model, position, TypeFormat);

        var resultType = selection.IsExpression
            ? plan.ExpressionType ?? expressionTypeFallback
            : ReturnedType(compilation, plan);
        var delegateType = DelegateType(compilation, parameterTypes, resultType);
        if (delegateType is null)
        {
            return null;
        }

        var header = $"(({Display(delegateType)})(static ({string.Join(", ", parameterNames)}) =>";
        var callSuffix = $"))({string.Join(", ", arguments)})";
        var selectedText = text.ToString(selection.Span);

        if (selection.IsExpression)
        {
            var bodyStart = selection.Span.Start + header.Length + 1;
            var body = Apply(selectedText, plan.ThisRewrites, selection.Span.Start);
            var declarations = TextSpan.FromBounds(selection.Span.Start, bodyStart);
            return new IifeText(selection.Span, $"{header} {body}{callSuffix}", new TextSpan(selection.Span.Start, 0), declarations, bodyStart, Relative(plan.ThisRewrites, selection.Span.Start));
        }

        return WriteStatements(text, plan, Display, header, callSuffix, selectedText);
    }

    static IifeText WriteStatements(SourceText text, IifePlan plan, Func<ITypeSymbol, string> display, string header, string callSuffix, string selectedText)
    {
        var selection = plan.Selection;
        var newLine = NewLineAt(text, selection.Span.Start);
        var indent = IndentationAt(text, selection.Span.Start);
        var bodyIndent = indent + IndentStep;

        var edits = Relative(plan.ThisRewrites, selection.Span.Start).ToList();
        if (!HasMultiLineLiteral(plan))
        {
            edits.AddRange(LineStartsNeedingIndent(selectedText).Select(start => new ThisRewrite(start, 0, IndentStep)));
        }

        edits.Sort((left, right) => left.Start.CompareTo(right.Start));

        var assignment = Assignment(plan, display);
        var prefix = $"{assignment}{header}{newLine}{indent}{{{newLine}";
        var localDeclarations = string.Concat(plan.VariablesDeclaredInLambda.Select(variable =>
            $"{bodyIndent}{display(IifePlan.TypeOf(variable))} {Escape(variable.Name)};{newLine}"));
        var declarationsEnd = selection.Span.Start + prefix.Length + localDeclarations.Length;
        var returnStatement = ReturnStatement(plan) is { } returned ? $"{newLine}{bodyIndent}{returned}" : string.Empty;
        var body = ApplyRelative(selectedText, edits);
        var newText = $"{prefix}{localDeclarations}{bodyIndent}{body}{returnStatement}{newLine}{indent}}}{callSuffix};";
        var declarations = TextSpan.FromBounds(selection.Span.Start + assignment.Length, declarationsEnd);
        var callSiteDeclarations = new TextSpan(selection.Span.Start, assignment.Length);
        return new IifeText(selection.Span, newText, callSiteDeclarations, declarations, declarationsEnd + bodyIndent.Length, edits);
    }

    static string Escape(string name) => SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    static ITypeSymbol? ReturnedType(Compilation compilation, IifePlan plan) => plan.VariablesReturned switch
    {
        [] => null,
        [var single] => IifePlan.TypeOf(single),
        var several => compilation.CreateTupleTypeSymbol(
            [.. several.Select(IifePlan.TypeOf)],
            elementNullableAnnotations: [.. several.Select(variable => IifePlan.TypeOf(variable).NullableAnnotation)]),
    };

    static INamedTypeSymbol? DelegateType(Compilation compilation, List<ITypeSymbol> parameterTypes, ITypeSymbol? resultType)
    {
        var typeArguments = resultType is null ? parameterTypes : [.. parameterTypes, resultType];
        var genericName = resultType is null ? "System.Action" : "System.Func";
        var metadataName = typeArguments.Count == 0 ? genericName : $"{genericName}`{typeArguments.Count}";
        var definition = compilation.GetTypeByMetadataName(metadataName);
        if (definition is null || typeArguments.Count == 0)
        {
            return definition;
        }

        return definition.Construct([.. typeArguments], [.. typeArguments.Select(type => type.NullableAnnotation)]);
    }

    static string Assignment(IifePlan plan, Func<ITypeSymbol, string> display)
    {
        string Target(ISymbol variable) => plan.VariablesDeclaredInSelectionAndReturned.Contains(variable)
            ? $"{display(IifePlan.TypeOf(variable))} {Escape(variable.Name)}"
            : Escape(variable.Name);

        return plan.VariablesReturned switch
        {
            [] => string.Empty,
            [var single] => $"{Target(single)} = ",
            var several => $"({string.Join(", ", several.Select(Target))}) = ",
        };
    }

    static string? ReturnStatement(IifePlan plan) => plan.VariablesReturned switch
    {
        [] => null,
        [var single] => $"return {Escape(single.Name)};",
        var several => $"return ({string.Join(", ", several.Select(variable => Escape(variable.Name)))});",
    };

    static string Apply(string selectedText, IReadOnlyList<ThisRewrite> rewrites, int selectionStart)
        => ApplyRelative(selectedText, Relative(rewrites, selectionStart));

    static IReadOnlyList<ThisRewrite> Relative(IReadOnlyList<ThisRewrite> rewrites, int selectionStart)
        => [.. rewrites.Select(rewrite => rewrite with { Start = rewrite.Start - selectionStart })];

    static string ApplyRelative(string selectedText, IReadOnlyList<ThisRewrite> edits)
    {
        var result = selectedText;
        foreach (var edit in edits.OrderByDescending(edit => edit.Start))
        {
            result = result.Remove(edit.Start, edit.RemovedLength).Insert(edit.Start, edit.Replacement);
        }

        return result;
    }

    /// <summary>Offsets of the starts of every non-blank line after the first.</summary>
    static IEnumerable<int> LineStartsNeedingIndent(string selectedText)
    {
        for (var index = 0; index < selectedText.Length; index++)
        {
            var isLineStart = selectedText[index] == '\n';
            var next = index + 1;
            if (isLineStart && next < selectedText.Length && selectedText[next] is not ('\r' or '\n'))
            {
                yield return next;
            }
        }
    }

    /// <summary>Indenting a line inside a multi-line string would change the string, so such selections keep their indentation.</summary>
    static bool HasMultiLineLiteral(IifePlan plan)
        => plan.Selection.TopLevelNodes
            .SelectMany(node => node.DescendantTokens())
            .Any(token => token.Text.Contains('\n', StringComparison.Ordinal) && !token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken));

    static string NewLineAt(SourceText text, int position)
    {
        var line = text.Lines.GetLineFromPosition(position);
        var lineBreak = text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        return lineBreak.Length > 0 ? lineBreak : Environment.NewLine;
    }

    static string IndentationAt(SourceText text, int position)
    {
        var line = text.Lines.GetLineFromPosition(position);
        var lineText = text.ToString(line.Span);
        return lineText[..(lineText.Length - lineText.TrimStart().Length)];
    }
}
