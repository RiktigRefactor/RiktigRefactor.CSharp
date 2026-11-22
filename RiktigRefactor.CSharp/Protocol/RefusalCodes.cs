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
    public const string InternalVerificationFailed = "internal-verification-failed";
}
