namespace RiktigRefactor.CSharp.Protocol;

public static class RefusalCodes
{
    public const string NoSymbol = "no-symbol";
    public const string NotRenameable = "not-renameable";
    public const string InvalidIdentifier = "invalid-identifier";
    public const string ConflictingDeclaration = "conflicting-declaration";
    public const string DynamicAccessPossible = "dynamic-access-possible";
    public const string StringNamesSymbol = "string-names-symbol";
    public const string BindingWouldChange = "binding-would-change";
    public const string WouldIntroduceErrors = "would-introduce-errors";
    public const string PreprocessorRegion = "preprocessor-region";
    public const string InvalidSelection = "invalid-selection";
    public const string NonLocalControlFlow = "non-local-control-flow";
    public const string ContainsAwait = "contains-await";
    public const string TypeNotNameable = "type-not-nameable";
    public const string TargetTypedExpression = "target-typed-expression";
    public const string SelectionIsVariable = "selection-is-variable";
    public const string ExpressionWritesVariable = "expression-writes-variable";
    public const string RefOrPointerAcrossBoundary = "ref-or-pointer-across-boundary";
    public const string BaseAccess = "base-access";
    public const string StructThis = "struct-this";
    public const string ThisNotRewritable = "this-not-rewritable";
    public const string StructMutationPossible = "struct-mutation-possible";
    public const string CapturedVariable = "captured-variable";
    public const string WritesLostOnException = "writes-lost-on-exception";
    public const string DeclarationEscapes = "declaration-escapes";
    public const string DisposalWouldMove = "disposal-would-move";
    public const string ExpressionTreeOrQuery = "expression-tree-or-query";
    public const string TooManyVariables = "too-many-variables";
    public const string InternalVerificationFailed = "internal-verification-failed";
}
