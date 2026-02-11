module FSharp.Compiler.Service.Tests.BreakpointLocationTests

open FSharp.Compiler.Text
open FSharp.Compiler.Text.Range
open FSharp.Test.Assert
open Xunit

let assertBreakpointRange ((startLine, startCol), (endLine, endCol)) markedSource =
    let context, parseResults = Checker.getParseResultsWithContext markedSource
    let breakpointRange = parseResults.ValidateBreakpointLocation(context.CaretPos).Value

    let expectedRange = mkFileIndexRange breakpointRange.FileIndex (Position.mkPos startLine startCol) (Position.mkPos endLine endCol)
    breakpointRange |> shouldEqual expectedRange

[<Fact>]
let ``Let - Function - Body 01`` () =
    assertBreakpointRange ((3, 4), (3, 5)) """
let f () =
    1{caret}
"""

[<Fact>]
let ``Seq 01`` () =
    assertBreakpointRange ((3, 4), (3, 5)) """
do
    1{caret}
    2
"""

[<Fact>]
let ``Seq 02`` () =
    assertBreakpointRange ((4, 4), (4, 5)) """
do
    1
    2{caret}
"""

[<Fact>]
let ``Lambda 01`` () =
    assertBreakpointRange ((2, 27), (2, 35)) """
[""] |> List.map (fun s -> s.Lenght{caret}) 
"""

[<Fact>]
let ``Dot lambda 01`` () =
    assertBreakpointRange ((2, 17), (2, 25)) """
[""] |> List.map _.Lenght{caret} 
"""
