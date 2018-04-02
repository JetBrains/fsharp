#if INTERACTIVE
#r "../../debug/fcs/net45/FSharp.Compiler.Service.dll" // note, run 'build fcs debug' to generate this, this DLL has a public API so can be used from F# Interactive
#r "../../packages/NUnit.3.5.0/lib/net45/nunit.framework.dll"
#load "FsUnit.fs"
#load "Common.fs"
#else
module Tests.Service.Symbols
#endif

open FSharp.Compiler.Service.Tests.Common
open FsUnit
open NUnit.Framework
open Microsoft.FSharp.Compiler.SourceCodeServices

let getAllSymbolUses (checkResults: FSharpCheckFileResults) =
    checkResults.GetAllUsesOfAllSymbolsInFile()
    |> Async.RunSynchronously

module ActivePatterns =

    let completePatternInput = """
let (|True|False|) = function
    | true -> True
    | false -> False

match true with
| True | False -> ()
"""

    let partialPatternInput = """
let (|String|_|) = function
    | :? String -> Some ()
    | _ -> None

match "foo" with
| String
| _ -> ()
"""

    let getCaseUsages source line =
        let fileName, options = mkTestFileAndOptions source [| |]
        let _, checkResults = parseAndCheckFile fileName source options
        let symbolUses = getAllSymbolUses checkResults
        
        symbolUses
        |> Array.filter (fun su -> su.RangeAlternate.StartLine = line && su.Symbol :? FSharpActivePatternCase)
        |> Array.map (fun su -> su.Symbol :?> FSharpActivePatternCase)

    [<Test>]
    let ``Active pattern case indices`` () =
        let getIndices = Array.map (fun (case: FSharpActivePatternCase) -> case.Index)

        getCaseUsages completePatternInput 7 |> getIndices |> shouldEqual [| 0; 1 |]
        getCaseUsages partialPatternInput 7 |> getIndices |> shouldEqual [| 0 |]

    [<Test>]
    let ``Active pattern group names`` () =
        let getGroupName (case: FSharpActivePatternCase) = case.Group.Name.Value

        getCaseUsages completePatternInput 7 |> Array.head |> getGroupName |> shouldEqual "|True|False|"
        getCaseUsages partialPatternInput 7 |> Array.head |> getGroupName |> shouldEqual "|String|_|"


module Symbols =

    [<Test>]
    let ``Is effectively same for different namespaces`` () =
        let source = """
namespace Ns1.Collections

namespace Ns2

open System.Collections
open Ns1.Collections
"""

        let fileName, options = mkTestFileAndOptions source [| |]
        let _, checkResults = parseAndCheckFile fileName source options
        let openedNamespaces =
            checkResults.OpenDeclarations
            |> Array.map (fun openDirective -> openDirective.Modules |> List.head)
            |> List.ofArray

        match openedNamespaces with
        | ns1 :: ns2 :: [] ->
            ns1.IsEffectivelySameAs(ns2) |> should be False
        | _ ->
            sprintf "Exprecting 2 namespaces, got: %A" openedNamespaces |> failwith