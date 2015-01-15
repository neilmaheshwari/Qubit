namespace PropertyTest

open System
open NUnit.Framework
open FSharp.Control.Reactive
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Concurrency
open Atom.Property
open Atom.Builders
open Microsoft.Reactive.Testing


    module TestUtility =

        type Record1 = 
            {
                FloatField : Property<float>
                IntField : Property<int>
            }

        type Record2 = 
            {
                NestedField : Property<Record1>
                StringField : Property<string>
            }

        let generateScheduledInts scheduler delayTicks initial final = 
            Observable.Interval(TimeSpan.FromTicks delayTicks, scheduler)
            |> Observable.map int
            |> Observable.map ((+) initial)
            |> Observable.take ((final + 1) - initial)

        let ``should equal list`` ys xs = 

            printfn "Result: %A"  <| Seq.toList xs
            printfn "Target: %A" ys

            xs
            |> Seq.toList
            |> (=) ys
            |> Assert.IsTrue

        let loopWithScheduler (scheduler : TestScheduler) steps delay fn = 
            for j in steps do
                scheduler.AdvanceBy delay
                fn ()