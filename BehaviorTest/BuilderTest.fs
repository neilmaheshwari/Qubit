namespace BehaviorBuilderTest

open System
open NUnit.Framework

open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open Nessos.FsPickler
open Nessos.FsPickler.Combinators

open Atom

open Atom.Behaviors
open Atom.Builders

type Record1 = 
    {
        FloatField : Behavior<float>
        IntField : Behavior<int>
    }

type Record2 = 
    {
        NestedField : Behavior<Record1>
        StringField : Behavior<string>
    }

[<TestFixture>]
type Builders() = 

    let generateTimedInts delay initial final =
        Observable.generateTimeSpan 
            initial 
            (Func<int, bool>(fun i -> i <= final))
            (Func<int,int>((+) 1)) 
            id 
            (fun _ -> TimeSpan (0, 0, delay))

    [<Test>]
    member x.BehaviorCETest() = 

        let delay = 1
        let intObs = generateTimedInts delay 1 2
        let stringObs = intObs |> Observable.map (fun x -> x.ToString())
        let xs = System.Collections.Generic.List<float*int*string>()
        let floatBehavior = returnC 109.0
        let stringBehavior = returnV "0" stringObs
        let intBehavior = returnV 0 intObs

        let record1Instance = 
            { 
                FloatField = floatBehavior 
                IntField = intBehavior
            } 

        let record2Instance = 
            {
                StringField = stringBehavior
                NestedField = returnC record1Instance
            } 
        
        for i in [1..2] do
            System.Threading.Thread.Sleep (delay * 1000)
            behaviorB {
                let! n = record2Instance.NestedField
                let! f = n.FloatField
                let! i = n.IntField
                let! s = record2Instance.StringField
                return (f, i, s)
            }
            |> value
            |> xs.Add
        
        printfn "xs: %A" xs

        xs
        |> Seq.toList
        |> (=) [ (109.0, 1, "1"); (109.0, 2, "2") ]
        |> Assert.IsTrue
