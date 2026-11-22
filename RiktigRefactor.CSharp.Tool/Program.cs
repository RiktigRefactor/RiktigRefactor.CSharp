using System.CommandLine;
using RiktigRefactor.CSharp.Tool;

var workspaceOption = new Option<string>("--workspace") { Description = "The .sln, .slnx or .csproj to load.", Required = true };
var fileOption = new Option<string>("--file") { Description = "The file containing the symbol.", Required = true };
var atOption = new Option<string>("--at") { Description = "The cursor position as line:column, both 1-based.", Required = true };
var newNameOption = new Option<string>("--new-name") { Description = "The new identifier.", Required = true };
var applyOption = new Option<bool>("--apply") { Description = "Write the changes to disk instead of only previewing them." };
var jsonOption = new Option<bool>("--json") { Description = "Print the result as JSON, using the shared protocol." };

var renameCommand = new Command("rename", "Rename a symbol and every reference to it. Exits 0 on success, 1 when refused, 2 on error.")
{
    workspaceOption,
    fileOption,
    atOption,
    newNameOption,
    applyOption,
    jsonOption,
};
renameCommand.SetAction((parseResult, cancellationToken) => RenameCommand.RunAsync(
    new RenameArguments(
        parseResult.GetRequiredValue(workspaceOption),
        parseResult.GetRequiredValue(fileOption),
        parseResult.GetRequiredValue(atOption),
        parseResult.GetRequiredValue(newNameOption),
        parseResult.GetValue(applyOption),
        parseResult.GetValue(jsonOption)),
    Console.Out,
    Console.Error,
    cancellationToken));

var rootCommand = new RootCommand("Behavior-preserving C# refactorings.") { renameCommand };
var parseResult = rootCommand.Parse(args);
var exitCode = await parseResult.InvokeAsync();
return parseResult.Errors.Count > 0 ? RenameCommand.ExitError : exitCode;
