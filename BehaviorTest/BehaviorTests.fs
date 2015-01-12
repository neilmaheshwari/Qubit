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
type Behaviors() = 

    let generateTimedInts delay initial final =
        Observable.generateTimeSpan 
            initial 
            (Func<int, bool>(fun i -> i <= final))
            (Func<int,int>((+) 1)) 
            id 
            (fun _ -> TimeSpan (0, 0, delay))

    [<Test>]
    member x.The_behavior_is_a_function_of_time()= 

        let delay = 1
        let obs = generateTimedInts delay 0 2
        let xs = System.Collections.Generic.List<int>()

        let behavior = returnV obs

        for i in [0..2] do
            xs.Add <| value behavior
            System.Threading.Thread.Sleep (delay * 1000)
            
        printfn "xs: %A" (xs |> Seq.toList)

        xs 
        |> Seq.toList
        |> (=) [0..2]
        |> Assert.IsTrue 

    [<Test>]
    member x.The_constant_behavior_is_constant() =  

        let behavior = returnC 1
        let xs = System.Collections.Generic.List<int>()

        for i in [0..1] do
            xs.Add <| value behavior
        
        printfn "xs: %A" xs

        xs
        |> Seq.toList
        |> (=) [1; 1]
        |> Assert.IsTrue

    [<Test>]
    member x.Applying_a_function_on_a_time_varying_behavior_works() = 

        let delay = 1
        let obs = generateTimedInts delay 0 3
        let xs = System.Collections.Generic.List<int>()

        let liftedAddition = Behaviors.fmap ((+) 1)

        let behavior = returnV obs |> liftedAddition

        for i in [0..3] do
            xs.Add <| value behavior
            System.Threading.Thread.Sleep (delay * 1000)
    
        printfn "xs: %A" (xs |> Seq.toList)

        xs 
        |> Seq.toList
        |> (=) [1..4]
        |> Assert.IsTrue 

    [<Test>]
    member x.Applying_a_function_on_a_constant_behavior_works() = 

        let xs = System.Collections.Generic.List<int>()
        let liftedAddition = Behaviors.fmap ((+) 1)
        let behavior = returnC 1 |> liftedAddition
        for i in [0..1] do
            xs.Add <| value behavior
        
        printfn "xs: %A" xs

        xs
        |> Seq.toList
        |> (=) [2; 2]
        |> Assert.IsTrue

