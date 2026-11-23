using System.CommandLine;
using RiktigRefactor.CSharp.Tool;

var workspaceOption = new Option<string>("--workspace") { Description = "The .sln, .slnx or .csproj to load.", Required = true };
var fileOption = new Option<string>("--file") { Description = "The file containing the symbol or selection.", Required = true };
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

var fromOption = new Option<string>("--from") { Description = "The start of the selection as line:column, both 1-based.", Required = true };
var toOption = new Option<string>("--to") { Description = "The end of the selection (exclusive) as line:column, both 1-based.", Required = true };
var extractIifeCommand = new Command("extract-iife", "Wrap the selected expression or statements in an immediately-invoked static lambda. Exits 0 on success, 1 when refused, 2 on error.")
{
    workspaceOption,
    fileOption,
    fromOption,
    toOption,
    applyOption,
    jsonOption,
};
extractIifeCommand.SetAction((parseResult, cancellationToken) => ExtractIifeCommand.RunAsync(
    new ExtractIifeArguments(
        parseResult.GetRequiredValue(workspaceOption),
        parseResult.GetRequiredValue(fileOption),
        parseResult.GetRequiredValue(fromOption),
        parseResult.GetRequiredValue(toOption),
        parseResult.GetValue(applyOption),
        parseResult.GetValue(jsonOption)),
    Console.Out,
    Console.Error,
    cancellationToken));

var receiverNameOption = new Option<string?>("--receiver-name") { Description = "The name of the new receiver parameter. Defaults to the type's name in camel case." };
var makeMethodStaticCommand = new Command("make-method-static", "Turn the instance method at the cursor into a static method that takes the receiver as its first parameter. Exits 0 on success, 1 when refused, 2 on error.")
{
    workspaceOption,
    fileOption,
    atOption,
    receiverNameOption,
    applyOption,
    jsonOption,
};
makeMethodStaticCommand.SetAction((parseResult, cancellationToken) => MakeMethodStaticCommand.RunAsync(
    new MakeMethodStaticArguments(
        parseResult.GetRequiredValue(workspaceOption),
        parseResult.GetRequiredValue(fileOption),
        parseResult.GetRequiredValue(atOption),
        parseResult.GetValue(receiverNameOption),
        parseResult.GetValue(applyOption),
        parseResult.GetValue(jsonOption)),
    Console.Out,
    Console.Error,
    cancellationToken));

var rootCommand = new RootCommand("Behavior-preserving C# refactorings.") { renameCommand, extractIifeCommand, makeMethodStaticCommand };
var parseResult = rootCommand.Parse(args);
var exitCode = await parseResult.InvokeAsync();
return parseResult.Errors.Count > 0 ? RefactoringCommand.ExitError : exitCode;
