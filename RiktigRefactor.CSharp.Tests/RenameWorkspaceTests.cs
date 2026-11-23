using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rename;
using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

/// <summary>Rename in workspaces the case format can't express: several projects, linked files, and source generators.</summary>
public class RenameWorkspaceTests
{
    const string Library = """
        namespace Lib;
        public class Base
        {
            public string Call() => Pick(1);
            public string Pick(object value) => "object";
            public string /*|*/Other(int value) => "int";
        }
        """;

    const string App = """
        class Program
        {
            static void Main()
            {
                if (new Lib.Base().Call() != "object") throw new System.Exception();
                if (Helper.Run() != "object") throw new System.Exception();
            }
        }
        static class Helper
        {
            public static string Run() => new Lib.Base().Pick(2);
        }
        """;

    [Fact]
    public async Task RenamesAcrossProjectReferences()
    {
        var solution = TwoProjects(Library.Replace("/*|*/", ""), "class Program { static void Main() { if (new Lib.Base().Other(1) != \"int\") throw new System.Exception(); } }");
        var result = await RenameAtAsync(solution, "Lib.cs", "Other", "Renamed");

        Assert.Equal(RefactoringStatus.Success, result.Status);
        Assert.Contains(result.Edits, edit => edit.File == "App.cs" && edit.NewText == "Renamed");
    }

    [Fact]
    public async Task RefusesWhenAReferencingProjectWouldBindToTheRenamedOverload()
    {
        var solution = TwoProjects(Library.Replace("/*|*/", ""), App);
        var result = await RenameAtAsync(solution, "Lib.cs", "Other", "Pick");

        Assert.Equal(RefactoringStatus.Refused, result.Status);
    }

    [Fact]
    public async Task RefusesWhenAFriendAssemblyWouldBindToTheRenamedInternalMember()
    {
        var library = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]
            namespace Lib;
            public class Base
            {
                public string Pick(object value) => "object";
                internal string Other(int value) => "int";
            }
            """;
        var solution = TwoProjects(library, App);
        var result = await RenameAtAsync(solution, "Lib.cs", "Other", "Pick");

        Assert.Equal(RefactoringStatus.Refused, result.Status);
    }

    [Fact]
    public async Task RefusesWhenGeneratedCodeMentionsTheNewName()
    {
        var program = """
            partial class Program
            {
                static string Pick(object value) => "object";
                static string /*|*/Other(int value) => "int";
                static void Main()
                {
                    if (Generated() != "object") throw new System.Exception();
                }
            }
            """.Replace("/*|*/", "");
        var solution = ProjectWithGenerator(program, "partial class Program { static string Generated() => Pick(1); }");

        var result = await RenameAtAsync(solution, "Program.cs", "Other", "Pick");

        Assert.Equal(RefactoringStatus.Refused, result.Status);
    }

    [Fact]
    public async Task RefusesWhenGeneratedCodeUsesAnImplicitlyLookedUpName()
    {
        var program = """
            using System.Collections;
            using System.Collections.Generic;
            partial class Program : IEnumerable
            {
                public List<string> Log { get; } = new();
                public void Add(object item) => Log.Add("object");
                public void Insert(int item) => Log.Add("int");
                public IEnumerator GetEnumerator() => Log.GetEnumerator();
                static void Main() { }
            }
            """;
        var solution = ProjectWithGenerator(program, "partial class Program { static Program Generated() => new Program { 1 }; }");

        var result = await RenameAtAsync(solution, "Program.cs", "Insert", "Add");

        Assert.Equal(RefactoringStatus.Refused, result.Status);
    }

    [Fact]
    public async Task RefusesWhenTheRenameChangesGeneratedOutputWithoutMentioningEitherName()
    {
        var solution = ProjectWithGenerator("partial class Program { static void Helper() { } static void Main() { Helper(); } }", "// unused");
        var project = solution.Projects.Single();
        solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(project.Id), "MemberNames.marker", SourceText.From(""), filePath: "MemberNames.marker");

        var result = await RenameAtAsync(solution, "Program.cs", "Helper", "Renamed");

        Assert.Equal(RefactoringStatus.Refused, result.Status);
        Assert.Equal(RefusalCodes.BindingWouldChange, Assert.Single(result.Reasons).Code);
    }

    [Fact]
    public async Task RefusesToRenameASymbolDeclaredInGeneratedCode()
    {
        var solution = ProjectWithGenerator(
            "partial class Program { static void Main() { Generated(); } }",
            "partial class Program { static void Generated() { } }");

        var result = await RenameAtAsync(solution, "Program.cs", "Generated", "Renamed");

        Assert.Equal(RefactoringStatus.Refused, result.Status);
    }

    [Fact]
    public async Task RenamesAMemberOfAPartialTypeWithAGeneratedPart()
    {
        var solution = ProjectWithGenerator(
            "partial class Program { static int Helper() => 1; static void Main() { if (Helper() + Generated() != 3) throw new System.Exception(); } }",
            "partial class Program { static int Generated() => 2; }");

        var result = await RenameAtAsync(solution, "Program.cs", "Helper", "Renamed");

        Assert.Equal(RefactoringStatus.Success, result.Status);
    }

    [Fact]
    public async Task EditsALinkedFileOnce()
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var first = AddProject(ref solution, "First");
        var second = AddProject(ref solution, "Second");
        var text = "class C { public int /*|*/M() => 1; public int N() => M(); }".Replace("/*|*/", "");
        solution = solution.AddDocument(DocumentId.CreateNewId(first), "Shared.cs", SourceText.From(text), filePath: "/src/Shared.cs");
        solution = solution.AddDocument(DocumentId.CreateNewId(second), "Shared.cs", SourceText.From(text), filePath: "/src/Shared.cs");

        var result = await RenameAtAsync(solution, "Shared.cs", "M", "Renamed");

        Assert.Equal(RefactoringStatus.Success, result.Status);
        Assert.Equal(2, result.Edits.Count);
    }

    [Fact]
    public async Task RefusesToEditAGeneratedFile()
    {
        var solution = TestWorkspace.CreateSolution(new Dictionary<string, string>
        {
            ["Program.cs"] = "class Program { static void Main() { Form.Run(); } }",
            ["Form.Designer.cs"] = "// <auto-generated/>\nstatic class Form { public static void Run() => Program2.Helper(); }",
            ["Program2.cs"] = "static class Program2 { public static void Helper() { } }",
        });

        var result = await RenameAtAsync(solution, "Program2.cs", "Helper", "Renamed");

        Assert.Equal(RefactoringStatus.Refused, result.Status);
        Assert.Equal(RefusalCodes.NotRenameable, Assert.Single(result.Reasons).Code);
    }

    static async Task<RefactoringResult> RenameAtAsync(Solution solution, string documentName, string name, string newName)
    {
        var document = solution.Projects.SelectMany(project => project.Documents).First(document => document.Name == documentName);
        var text = (await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        var position = text.IndexOf(" " + name + "(", StringComparison.Ordinal) + 1;
        return await RenameRefactoring.RenameAsync(solution, document.Id, position, newName, TestContext.Current.CancellationToken);
    }

    static Solution TwoProjects(string library, string app)
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var lib = AddProject(ref solution, "Lib", OutputKind.DynamicallyLinkedLibrary);
        var appProject = AddProject(ref solution, "App");
        solution = solution.AddProjectReference(appProject, new ProjectReference(lib));
        solution = solution.AddDocument(DocumentId.CreateNewId(lib), "Lib.cs", SourceText.From(library), filePath: "Lib.cs");
        return solution.AddDocument(DocumentId.CreateNewId(appProject), "App.cs", SourceText.From(app), filePath: "App.cs");
    }

    static Solution ProjectWithGenerator(string program, string generated)
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var project = AddProject(ref solution, "App");
        solution = solution.AddAnalyzerReference(project, new AnalyzerFileReference(typeof(AdditionalFilesAsSourceGenerator).Assembly.Location, new TestAssemblyLoader()));
        solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(project), "Generated.txt", SourceText.From(generated), filePath: "Generated.txt");
        return solution.AddDocument(DocumentId.CreateNewId(project), "Program.cs", SourceText.From(program), filePath: "Program.cs");
    }

    static ProjectId AddProject(ref Solution solution, string name, OutputKind kind = OutputKind.ConsoleApplication)
    {
        var id = ProjectId.CreateNewId();
        solution = solution.AddProject(ProjectInfo.Create(
            id,
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(kind),
            parseOptions: ProgramRunner.ParseOptions,
            metadataReferences: ProgramRunner.References));
        return id;
    }

    class TestAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
        }

        public System.Reflection.Assembly LoadFromPath(string fullPath) => typeof(TestAssemblyLoader).Assembly;
    }
}

