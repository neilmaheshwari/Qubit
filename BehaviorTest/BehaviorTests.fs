namespace BehaviorTest

open System
open NUnit.Framework

open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open Nessos.FsPickler
open Nessos.FsPickler.Combinators

open Atom

[<TestFixture>]
type Test() = 

    [<Test>]
    member x.TestCase() = 

        let aggregator = new System.Collections.Generic.List<int*int>()

        let obs = System.Reactive.Linq.Observable.Repeat 0

        let prop = new Property<int> (obs, 100)

        obs
        |> Observable.takeWhile (fun _ -> false)
        |> Observable.map (fun x -> 
            aggregator.Add (x, 1)
            x)
        |> Observable.take 5
        |> Observable.subscribe ignore
        |> ignore

        printfn "aggregator: %A" aggregator

        Assert.IsTrue false

