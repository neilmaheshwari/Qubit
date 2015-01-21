namespace PropertyTest

open System
open NUnit.Framework

open System.Collections.Generic
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Concurrency
open Atom.Property
open Atom.Builders
open Microsoft.Reactive.Testing
open PropertyTest.TestUtility

[<TestFixture>]
type Test() = 

    let channel = "channelA"

    [<Test>]
    member x.``When there is a subscriber, the remoteObservable pushes values``() = 

        let scheduler = new TestScheduler()
        let obs = generateScheduledInts scheduler (int64 1) 0 1
                  
        let channel = "channelB"

        let xs = System.Collections.Generic.List<int>()

        Atom.Observer.remote channel obs

        let remoteObservable = Atom.Observable.remote channel

        let subscription = 
            remoteObservable.Observable
            |> Observable.subscribe xs.Add

        scheduler.AdvanceBy (int64 1)

        xs |> ``should equal list`` [0]

    [<Test>]
    member x.``When there is not a subscriber, the remoteObservable does not push a value``() =

        let scheduler = new TestScheduler ()

        let obs = generateScheduledInts scheduler (int64 1) 0 1

        let ys = System.Collections.Generic.List<int>()

        Atom.Observer.remote channel obs

        let remoteObservable = Atom.Observable.remote channel

        remoteObservable.Observable
        |> Observable.iter ys.Add
        |> ignore

        scheduler.AdvanceBy (int64 1)

        ys |> ``should equal list`` []

    [<Test>]
    member x.``Replaying observable works``() = 


        let scheduler = new TestScheduler ()
        let obs = generateScheduledInts scheduler (int64 2) 0 1
                  |> fun x -> Observable.Do (x, printfn "Doing... %A")

        let xs = System.Collections.Generic.List<int>()
        Atom.Observer.remote channel obs
        let remoteObservable = Atom.Observable.remote channel

        let refCountConn = 
            remoteObservable.Observable
            |> Observable.replayBuffer 1
        
        let conn = refCountConn.Connect()

        scheduler.AdvanceBy (int64 2)

        let subscription =
            refCountConn
            |> Observable.subscribe xs.Add

        xs |> ``should equal list`` [0]

    [<Test>]
    member x.``Replaying observables switching streams``() =

        let channel = "channelC"
        let scheduler = new TestScheduler()
        let xs = System.Collections.Generic.List<int>()

        let obs = 
            generateScheduledInts scheduler (int64 10) 0 10
            |> Observable.map (
                fun x ->
                    {
                        SilverMineLevel = Level x
                    })
            |> Observable.iter (printfn "Generating homebase: %A")                    
            
        Atom.Observer.remote channel obs
        let remoteObservable = Atom.Observable.remote channel

        let generateSilverTime level =
            let (Level l) = level
            let initial = l * 100
            let delay = int64 1
            generateScheduledInts scheduler delay initial (initial + 99)
            |> Observable.map SilverTime
            |> Observable.iter (printfn "%A mine generating: %A" level)

        let o = 
            remoteObservable.Observable
            |> Observable.Publish

        o.Connect() |> ignore

        let replay (obs : IObservable<'T>) =
            let replayed = 
                obs 
                |> fun x -> x.Replay 1
            replayed.Connect() |> ignore
            replayed

        let replayed = replay o

        scheduler.AdvanceBy (int64 11)

        let observeSilverTime = 
            observe {
                let! { SilverMineLevel = level } = replayed
                let! silverTime = 
                    generateSilverTime level 
                    |> Observable.takeUntilOther o
                return silverTime
            }

        observeSilverTime
        |> Observable.subscribe (
            fun (x : SilverTime) ->
                printfn "SilverTime : %A" x)
        |> ignore

        scheduler.AdvanceBy (int64 1000)

        Assert.Fail()