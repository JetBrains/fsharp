module GetTypeOfExpressionTests

open Xunit
open FSharp.Compiler.Text
open FSharp.Test.Compiler

[<Fact>]
let ``Record type in Computation expression`` () =
    FSharp """
type Entry =
    {
        Idx: int
        FileName: string
        /// Own deps
        DependencyCount: int
        /// Being depended on
        DependentCount: int
        LineCount: int
    }

let x =
    {
        Entry.ReSharperFSharpRulezzz
    }
"""
    |> typecheckResults
    |> fun typeCheckResults ->
        let anyRange = typeCheckResults.GetAllUsesOfAllSymbolsInFile() |> Seq.head |> fun symbolUse -> symbolUse.Range
        let mExpr = Range.mkRange anyRange.FileName (Position.mkPos 14 4) (Position.mkPos 16 5)
        let t = typeCheckResults.GetTypeOfExpression(mExpr).Value
        Assert.True t.TypeDefinition.IsFSharpRecord
        
[<Fact>]
let ``Qualified RecordType name in computation expression`` () =
    FSharp """
module Module1 =
    type R1 =
        { Field1: int }

    type R2 =
        { Field2: int }

module Module2 =
    { Module1.ReSharperFSharpRulezzz } // Both Field1 and Field2 should be available, we can't assume any record type and report it

    { R1.ReSharperFSharpRulezzz } // Field1 should be available, _maybe_ we can assume the record type for the completion fix, but it's a wrong hack, because `R1` is not an expression of `R1` type. Reporting it would enable things like `let` and `with` templates and features like Introduce Variable would also see it wrongly

    { Module1.R1.ReSharperFSharpRulezzz } // Field1 is actually available from FCS completion, maybe we could check why it works
"""
    |> typecheckResults
    |> fun typeCheckResults ->
        let anyRange = typeCheckResults.GetAllUsesOfAllSymbolsInFile() |> Seq.head |> fun symbolUse -> symbolUse.Range
        let m1 = Range.mkRange anyRange.FileName (Position.mkPos 10 4) (Position.mkPos 10 38)
        let m2 = Range.mkRange anyRange.FileName (Position.mkPos 12 4) (Position.mkPos 12 33)
        let m3 = Range.mkRange anyRange.FileName (Position.mkPos 14 4) (Position.mkPos 14 41)
        
        let t1 = typeCheckResults.GetTypeOfExpression(m1).Value
        Assert.False t1.TypeDefinition.IsFSharpRecord
        let t2 = typeCheckResults.GetTypeOfExpression(m2).Value
        Assert.False t2.TypeDefinition.IsFSharpRecord
        let t3 = typeCheckResults.GetTypeOfExpression(m3).Value
        Assert.True t3.TypeDefinition.IsFSharpRecord
