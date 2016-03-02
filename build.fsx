#r @"packages\build\FAKE\tools\FakeLib.dll"
open System
open Fake 
open Fake.AssemblyInfoFile

let releaseNotes = 
    ReadFile "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let buildMode = getBuildParamOrDefault "buildMode" "Release"
let buildVersion = getBuildParamOrDefault "buildVersion" "0.0.1"
let nugetApikey = getBuildParamOrDefault "nugetApikey" ""

let fileListUnitTests = !! ("**/bin/" @@ buildMode @@ "/TheBorg*Tests.dll")
let toolNUnitDir = "./packages/build/NUnit.Runners/tools"
let toolIlMerge = "./packages/build/ilmerge/tools/ILMerge.exe"
let nugetVersion = buildVersion // + "-alpha"
let nugetVersionDep = "["+nugetVersion+"]"


Target "Clean" (fun _ ->
    CleanDirs [ ]
    )

Target "SetVersion" (fun _ ->
    CreateCSharpAssemblyInfo "./Source/SolutionInfo.cs"
        [Attribute.Version buildVersion
         Attribute.InformationalVersion nugetVersion
         Attribute.FileVersion buildVersion]
    )

Target "BuildApp" (fun _ ->
    MSBuild null "Build" ["Configuration", buildMode] ["./TheBorg.sln"]
    |> Log "AppBuild-Output: "
    )

Target "UnitTest" (fun _ ->
    fileListUnitTests
        |> NUnit (fun p -> 
            {p with
                DisableShadowCopy = true;
                Framework = "net-4.0";
                ToolPath = toolNUnitDir;
                TimeOut = TimeSpan.FromMinutes 30.0;
                ToolName = "nunit-console-x86.exe";
                OutputFile = "nunit.xml"})
    )


Target "Default" DoNothing

"Clean"
    ==> "SetVersion"
    ==> "BuildApp"
//    ==> "UnitTest"
//    ==> "CreateRelease"
    ==> "Default"

RunTargetOrDefault "Default"
