open System
open System.Collections.Generic
open Fake
open Fake.Core
open Fake.IO
open System.Threading


let (</>) x y = System.IO.Path.Combine(x, y);


let run workingDir fileName args =
    printfn $"CWD: %s{workingDir}"
    let fileName, args =
        if Environment.isUnix
        then fileName, args else "cmd", ("/C " + fileName + " " + args)

    CreateProcess.fromRawCommandLine fileName args
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.withTimeout TimeSpan.MaxValue
    |> CreateProcess.ensureExitCodeWithMessage $"'%s{workingDir}> %s{fileName} %s{args}' task failed"
    |> Proc.run
    |> ignore

let execStdout workingDir fileName args =
    printfn $"CWD: %s{workingDir}"
    let fileName, args =
        if Environment.isUnix
        then fileName, args else "cmd", ("/C " + fileName + " " + args)

    CreateProcess.fromRawCommandLine fileName args
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.withTimeout TimeSpan.MaxValue
    |> CreateProcess.redirectOutput
    |> CreateProcess.ensureExitCodeWithMessage $"'%s{workingDir}> %s{fileName} %s{args}' task failed"
    |> Proc.run
    |> fun result -> result.Result.Output


let dotnet = "dotnet"

open System.IO
open System.Linq

/// Recursively tries to find the parent of a file starting from a directory
let rec findParent (directory: string) (fileToFind: string) = 
    let path = if Directory.Exists(directory) then directory else Directory.GetParent(directory).FullName
    let files = Directory.GetFiles(path)
    if files.Any(fun file -> Path.GetFileName(file).ToLower() = fileToFind.ToLower()) 
    then path 
    else findParent (DirectoryInfo(path).Parent.FullName) fileToFind
    
let cwd = findParent __SOURCE_DIRECTORY__ "README.md"

let src = cwd </> "src" </> "Giraffe.GoodRead"
let tests = cwd </> "tests" </> "Giraffe.GoodRead.Tests"

let clean projectPath = Shell.cleanDirs [ projectPath </> "bin" projectPath </> "obj" ]

let targets = Dictionary<string, TargetParameter -> unit>()

let createTarget name run = targets.Add(name, run)

let publish projectPath = fun _ ->
    clean projectPath
    "pack -c Release"
    |> run projectPath dotnet
    let nugetKey =
        match Environment.environVarOrNone "NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a NUGET_KEY environmental variable"
    let nupkg = System.IO.Directory.GetFiles(projectPath </> "bin" </> "Release") |> Seq.head
    let pushCmd = $"nuget push %s{nupkg} -s nuget.org -k %s{nugetKey}"
    run projectPath dotnet pushCmd

createTarget "Publish" (publish src)

createTarget "Test" (fun _ ->
    clean tests
    run tests dotnet "run"
)

let runTarget targetName =
    if targets.ContainsKey targetName then
        let input = Unchecked.defaultof<TargetParameter>
        targets[targetName] input
    else
        printfn $"Could not find build target {targetName}"

[<EntryPoint>]
let main(args: string[]) =
    match args with
    | [| targetName |] -> runTarget targetName
    | otherwise -> printfn $"Unknown args %A{otherwise}"
    0