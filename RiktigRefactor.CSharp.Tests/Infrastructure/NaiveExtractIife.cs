using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.ExtractIife;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// Wraps the selection in a static lambda the same way the real refactoring does, but with no preconditions and no verification.
/// Edge-case tests must fail under this implementation, which proves they can detect a behavior change.
/// </summary>
public static class NaiveExtractIife
{
    public static async Task<Solution> ExtractAsync(Solution solution, DocumentId documentId, TextSpan selectedSpan)
    {
        var document = solution.GetDocument(documentId)!;
        var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync().ConfigureAwait(false);
        var text = await document.GetTextAsync().ConfigureAwait(false);
        var selection = IifeSelection.Find(root!, selectedSpan) ?? throw new InvalidOperationException("The selection is not an expression or statements.");
        var plan = IifePlanner.Plan(model!, selection, CancellationToken.None) ?? throw new InvalidOperationException("No data flow for the selection.");
        var fallbackType = selection.Expression is { } expression ? model!.GetTypeInfo(expression).ConvertedType ?? model!.Compilation.ObjectType : null;
        var iife = IifeWriter.Write(model!, text, plan, fallbackType) ?? throw new InvalidOperationException("Too many variables.");
        return document.WithText(text.WithChanges(new TextChange(iife.ReplacedSpan, iife.NewText))).Project.Solution;
    }
}
