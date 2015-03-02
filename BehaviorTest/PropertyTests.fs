namespace PropertyTest

open System
open NUnit.Framework

open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Concurrency
open Atom.Property
open Atom.Builders
open Microsoft.Reactive.Testing
open PropertyTest.TestUtility


[<TestFixture>]
type Properties() = 

    [<Test>]
    member x.``The property is a function of time``()= 

        let scheduler = new TestScheduler()
        let delay = (int64) 1
        let obs = generateScheduledInts scheduler delay 10 11
        let xs = System.Collections.Generic.List<int>()

        let property = returnV 9 obs

        let fn _ = xs.Add <| value property

        loopWithScheduler scheduler [1..2] delay fn
            
        xs |> ``should equal list`` [10..11]

    [<Test>]
    member x.``The constant property is constant``() =  

        let property = returnC 1
        let xs = System.Collections.Generic.List<int>()

        let fn _ = xs.Add <| value property

        loopWithScheduler (new TestScheduler()) [0..1] (int64 0) fn 

        xs |> ``should equal list`` [1; 1]

    [<Test>]
    member x.``Applying a function on a time varying property works``() = 

        let scheduler = new TestScheduler()
        let delay = (int64) 1
        let obs = generateScheduledInts scheduler delay 1 3
        let xs = System.Collections.Generic.List<int>()
        let ys = System.Collections.Generic.List<int>()

        let liftedAddition = fmap <| (+) 1

        let property1 = returnV 0 obs |> liftedAddition
        let property2 = returnV 0 obs

        let fn _ = 
            ys.Add <| value property2
            xs.Add <| value property1

        loopWithScheduler scheduler [1..3] delay fn

        printfn "xs: %A" (xs |> Seq.toList)
        printfn "ys: %A" (ys |> Seq.toList)

        let xs' = Seq.toList xs
        let ys' = Seq.toList ys

        List.zip xs' ys'
        |> List.map (fun (x,y) -> x - y)
        |> List.filter ((<>) 1)
        |> List.isEmpty
        |> Assert.IsTrue


    [<Test>]
    member x.``Applying a function on a constant property works``() = 

        let xs = System.Collections.Generic.List<int>()
        let liftedAddition = fmap <| (+) 1
        let property = returnC 1 |> liftedAddition

        let fn _ = xs.Add <| value property

        loopWithScheduler (new TestScheduler()) [0..1] (int64 0) fn

        printfn "xs: %A" xs

        xs |> ``should equal list`` [2; 2]

    [<Test>]
    member x.``Exposing underlying observables works for constant``() = 

        let xs = System.Collections.Generic.List<int>()
        let p = returnC 123
        let obs = exposeStream p
        let sub = Observable.subscribe xs.Add obs
        sub.Dispose()

        xs |> ``should equal list`` [123]

    [<Test>]
    member x.``Exposing underlying observable works for varying``() = 

        let xs = System.Collections.Generic.List<int>()
        let ys = System.Collections.Generic.List<int>()

        let scheduler = new TestScheduler()
        let delay = int64 1
        let obs = 
            generateScheduledInts scheduler delay 1 2
            |> fun x -> 
                Observable.Do (x, fun o ->
                    ys.Add o)
        
        let p = returnV 0 obs

        let exposedStream = exposeStream p
        let sub1 = Observable.subscribe xs.Add exposedStream
        let sub2 = Observable.subscribe ignore exposedStream

        loopWithScheduler scheduler [0..1] delay ignore

        sub1.Dispose ()
        sub2.Dispose ()

        xs |> ``should equal list`` (Seq.toList ys)
