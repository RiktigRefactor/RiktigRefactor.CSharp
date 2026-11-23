using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class InteropRenameCaseTests
{
    // The entry point is already explicit, so only the method name changes.
    [Fact]
    public Task DllimportExplicitEntryPoint()
        => RenameCase.InDirectory("interop/dllimport-explicit-entry-point")
            .RenameTo("MeasureText")
            .RunAsync();

    // With no EntryPoint, DllImport calls the native function named like the method, so the rename has to write the old name as the entry point.
    [Fact]
    public Task DllimportImplicitEntryPoint()
        => RenameCase.InDirectory("interop/dllimport-implicit-entry-point")
            .RenameTo("MeasureText")
            .NaiveShouldFail()
            .RunAsync();
}
