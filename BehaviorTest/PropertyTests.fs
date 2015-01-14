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

[<TestFixture>]
type Properties() = 

    let generateTimedInts delay initial final =
        Observable.generateTimeSpan 
            initial 
            (Func<int, bool>(fun i -> i <= final))
            (Func<int,int>((+) 1)) 
            id 
            (fun _ -> TimeSpan (0, 0, delay))

    [<Test>]
    member x.The_property_is_a_function_of_time()= 

        let delay = 1
        let obs = generateTimedInts delay 10 11
                  |> fun x -> Observable.Do (x, fun i -> 
                      printfn "Publishing %A" i)
        let xs = System.Collections.Generic.List<int>()

        let property = returnV 9 obs

        for i in [9..11] do
            printfn "Value: %A" (value property)
            xs.Add <| value property
            System.Threading.Thread.Sleep (delay * 1000)
            
        printfn "xs: %A" (xs |> Seq.toList)

        xs 
        |> Seq.toList
        |> (=) [9..11]
        |> Assert.IsTrue 

    [<Test>]
    member x.The_constant_property_is_constant() =  

        let property = returnC 1
        let xs = System.Collections.Generic.List<int>()

        for i in [0..1] do
            xs.Add <| value property
        
        printfn "xs: %A" xs

        xs
        |> Seq.toList
        |> (=) [1; 1]
        |> Assert.IsTrue

    [<Test>]
    member x.Applying_a_function_on_a_time_varying_property_works() = 

        let delay = 1
        let obs = generateTimedInts delay 1 3
        let xs = System.Collections.Generic.List<int>()
        let ys = System.Collections.Generic.List<int>()

        let liftedAddition = fmap <| (+) 1

        let property1 = returnV 0 obs |> liftedAddition
        let property2 = returnV 0 obs

        for i in [1..3] do
            ys.Add <| value property2
            xs.Add <| value property1
            System.Threading.Thread.Sleep (delay * 1000)
    
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
    member x.Applying_a_function_on_a_constant_property_works() = 

        let xs = System.Collections.Generic.List<int>()
        let liftedAddition = fmap <| (+) 1
        let property = returnC 1 |> liftedAddition
        for i in [0..1] do
            xs.Add <| value property
        
        printfn "xs: %A" xs

        xs
        |> Seq.toList
        |> (=) [2; 2]
        |> Assert.IsTrue

    [<Test>]
    member x.Exposing_underlying_observables_works_for_constant() = 

        let xs = System.Collections.Generic.List<int>()

        let p = returnC 123

        let obs = 
            p
            |> exposeStream

        printfn "Obs: %A" obs

        obs
        |> Observable.subscribe xs.Add
        |> ignore

        Assert.IsTrue (xs |> Seq.toList |> (=) [123])

    [<Test>]
    member x.Exposing_underlying_observable_works_for_varying () = 

        let delay = 1

        let xs = System.Collections.Generic.List<int>()
        let ys = System.Collections.Generic.List<int>()

        let obs = 
            generateTimedInts delay 1 2
            |> fun x -> Observable.Do (x, fun o ->
                ys.Add o
                printfn "Publishing: %A" o)

        let p = returnV 0 obs

        let exposedStream = 
            p 
            |> exposeStream

        let sub1 = 
            exposedStream
            |> Observable.subscribe xs.Add
                
        let sub2 = 
            exposedStream
            |> Observable.subscribe (fun y ->
                printfn "Sub2: %A" y)

        System.Threading.Thread.Sleep (2 * 1000)

        let xs' = xs |> Seq.toList

        let ys' = ys |> Seq.toList

        ys' = xs'
        |> Assert.IsTrue

    [<Test>]
    member x.HotObservables () =

        let xs = System.Collections.Generic.List<int64>()

        let obs = 
            TimeSpan.FromSeconds 1.0
            |> Observable.interval
            |> fun x -> Observable.Do (x, fun i -> printfn "Publishing: %A" i)
                 
        let refCountObservables = 
            obs
            |> fun x -> x.Replay 1
            |> Observable.refCount

        System.Threading.Thread.Sleep 1100

        let obsConnection = refCountObservables |> Observable.subscribe (printfn "Sub 1: %A")
        let obsConnection2 = refCountObservables |> Observable.subscribe (printfn "Sub 2: %A")

        System.Threading.Thread.Sleep 5000

//        let obsConnection3 = refCountObservables |> Observable.subscribe (printfn "Sub 3: %A")
//        let obsConnection4 = refCountObservables |> Observable.subscribe (printfn "Sub 4: %A")

        System.Threading.Thread.Sleep 2000

        obsConnection.Dispose()
        obsConnection2.Dispose()
//        obsConnection3.Dispose()
//        obsConnection4.Dispose()

        System.Threading.Thread.Sleep 2000

        let obsConnection5 = refCountObservables |> Observable.subscribe (printfn "Sub 5: %A")

        System.Threading.Thread.Sleep 2000

        obsConnection5.Dispose()

        xs
        |> Seq.toList
        |> List.map (fun i -> int i)
        |> (=) [1..2]
        |> Assert.IsTrue 