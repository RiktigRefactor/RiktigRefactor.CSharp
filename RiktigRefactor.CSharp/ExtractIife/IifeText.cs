using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.ExtractIife;

/// <summary>The text that replaces the selection, and how offsets in the selection map into it.</summary>
/// <param name="ReplacedSpan">The span of the original document that <paramref name="NewText"/> replaces.</param>
/// <param name="NewText">The immediately-invoked lambda.</param>
/// <param name="CallSiteDeclarationsSpan">The span, relative to the new document, of the call site's assignment, which declares the variables the selection declared and the code after it uses.</param>
/// <param name="DeclarationsSpan">The span, relative to the new document, holding the lambda's parameters and the locals it declares for variables from outside.</param>
/// <param name="BodyStart">The offset, in the new document, where the selection's own text starts.</param>
/// <param name="BodyEdits">Edits applied to the selection's text, as offsets relative to the selection's start.</param>
public record IifeText(TextSpan ReplacedSpan, string NewText, TextSpan CallSiteDeclarationsSpan, TextSpan DeclarationsSpan, int BodyStart, IReadOnlyList<ThisRewrite> BodyEdits)
{
    public int Delta => NewText.Length - ReplacedSpan.Length;

    /// <summary>Maps an offset in the original document to the same character in the new one. Offsets inside the selection move into the lambda body.</summary>
    public int MapOffset(int oldOffset)
    {
        if (oldOffset < ReplacedSpan.Start)
        {
            return oldOffset;
        }

        if (oldOffset >= ReplacedSpan.End)
        {
            return oldOffset + Delta;
        }

        var relative = oldOffset - ReplacedSpan.Start;
        var shift = BodyEdits
            .Where(edit => edit.RemovedLength == 0 ? edit.Start <= relative : edit.Start + edit.RemovedLength <= relative)
            .Sum(edit => edit.Replacement.Length - edit.RemovedLength);
        return BodyStart + relative + shift;
    }
}
