namespace PropertyBuilderTest

open System
open NUnit.Framework

open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open System.Reactive.Linq

open Atom.Property
open Atom.Builders

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
    member x.PropertyCETest() = 

        let delay = 1
        let intObs = generateTimedInts delay 1 2
        let stringObs = intObs |> Observable.map (fun x -> x.ToString())
        let xs = System.Collections.Generic.List<float*int*string>()
        let floatProperty = returnC 109.0
        let stringProperty = returnV "0" stringObs
        let intProperty = returnV 0 intObs

        let record1Instance = 
            { 
                FloatField = floatProperty 
                IntField = intProperty
            } 

        let record2Instance = 
            {
                StringField = stringProperty
                NestedField = returnC record1Instance
            } 

        for j in [1..2] do
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

    [<Test>]
    member x.FmapBuilder() = 

        let delay = 1
        let intObs = 
            generateTimedInts delay 1 2

        let xs = System.Collections.Generic.List<int>()
        let floatProperty = returnC 109.0
        let stringProperty = returnC "String"
        let intProperty = returnV 0 intObs

        let record1Instance = 
            { 
                FloatField = floatProperty 
                IntField = intProperty
            } 

        let record1Property = 
            returnV record1Instance (Observable.Return record1Instance)

        let record2Instance = 
            {
                StringField = stringProperty
                NestedField = record1Property
            } 

        let record2 = returnC record2Instance

        let n = 
            behaviorFmapBuilder {
                let! r = record2
                let! record1 = r.NestedField
                return! record1.IntField
            }

        for i in [1..2] do
            System.Threading.Thread.Sleep (delay * 1000)
            xs.Add <| value n

        printfn "XS: %A" xs

        xs
        |> Seq.toList
        |> (=) [1..2]
        |> Assert.IsTrue