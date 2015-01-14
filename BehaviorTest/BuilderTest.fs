namespace PropertyBuilderTest

open System
open NUnit.Framework

open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open System.Reactive.Linq
open Microsoft.Reactive.Testing
open Atom.Property
open Atom.Builders
open PropertyTest.TestUtility

[<TestFixture>]
type Builders() = 

    [<Test>]
    member x.PropertyCETest() = 

        let delay = int64 1
        let scheduler = new TestScheduler()
        let intObs = generateScheduledInts scheduler delay 1 2
        let stringObs = intObs |> Observable.map string
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
        
        let fn _ = 
            propertyB {
                let! n = record2Instance.NestedField
                let! f = n.FloatField
                let! i = n.IntField
                let! s = record2Instance.StringField
                return (f, i, s)                
            }
            |> value
            |> xs.Add

        loopWithScheduler scheduler [1..2] delay fn

        xs |> ``should equal list`` [ (109.0, 1, "1"); (109.0, 2, "2") ]

    [<Test>]
    member x.FmapBuilder() = 

        let delay = int64 1
        let scheduler = new TestScheduler()
        let intObs = generateScheduledInts scheduler delay 1 2

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
            propertyFmapBuilder {
                let! r = record2
                let! record1 = r.NestedField
                return! record1.IntField
            }

        let fn _ = xs.Add <| value n
            
        loopWithScheduler scheduler [1..2] delay fn

        xs |> ``should equal list`` [1..2]
