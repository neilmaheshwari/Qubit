namespace BehaviorTest

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

[<TestFixture>]
type Test() = 

    let generateTimedInts delay initial final =
        Observable.generateTimeSpan 
            initial 
            (Func<int, bool>(fun i -> i <= final))
            (Func<int,int>((+) 1)) 
            id 
            (fun _ -> TimeSpan (0, 0, delay))

    [<Test>]
    member x.``The behavior is a function of time.``()= 

        let delay = 1
        let obs = generateTimedInts delay 0 1
        let xs = System.Collections.Generic.List<int>()

        let behavior = returnV obs

        System.Threading.Thread.Sleep (delay * 1000)

        for i in [0..1] do
            xs.Add <| value behavior
            System.Threading.Thread.Sleep (delay * 1000)
    
        printfn "xs: %A" xs

        xs 
        |> Seq.toList
        |> (=) [0..1]
        |> Assert.IsTrue 

    [<Test>]
    member x.``The constant behavior is constant``() =  

        let behavior = returnC 1
        let xs = System.Collections.Generic.List<int>()

        for i in [0..1] do
            xs.Add <| value behavior
        
        printfn "xs: %A" xs

        xs
        |> Seq.toList
        |> (=) [1; 1]
        |> Assert.IsTrue
